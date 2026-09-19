using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Application.Presentation;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Core.Time;
using Cultures.History;
using Cultures.Population;
using Cultures.World;

namespace Cultures.Tests;

public sealed class AlphaSessionTests
{
    [Fact]
    public void Clock_speed_and_autosave_are_session_state()
    {
        var host = new SimulationHost(
            1,
            populationCount: 1,
            placeDevelopmentBuildings: false,
            balance: new SimulationBalance { AutosaveIntervalTicks = 4, HuntFoodYield = 2 });
        host.Clock.SetSpeed(4);
        host.OnboardingComplete = true;
        host.Step(8);
        Assert.NotNull(host.LastAutosave);
        Assert.Equal(8UL, host.Clock.Tick);
        Assert.True(host.Diagnostics.LastStepMilliseconds >= 0);
        Assert.Null(host.Diagnostics.LastFault);

        var restored = SimulationHost.FromSave(host.CreateSave());
        Assert.Equal(4, restored.Clock.Speed);
        Assert.True(restored.OnboardingComplete);
        Assert.Equal(4, restored.CreateSave().SaveVersion);
    }

    [Fact]
    public void Memory_save_store_roundtrips_without_a_scene()
    {
        var store = new MemorySaveStore();
        var host = new SimulationHost(5, populationCount: 2);
        host.WriteSave(store, SaveSlots.Default);
        var loaded = SimulationHost.LoadSave(store, SaveSlots.Default);
        Assert.Equal(host.WorldSeed, loaded.WorldSeed);
        Assert.Equal(host.Population.Count, loaded.Population.Count);
        Assert.Equal(host.Population[0].Id, loaded.Population[0].Id);
    }

    [Fact]
    public void V3_envelope_migrates_to_v4()
    {
        var json = """
            {"saveVersion":3,"worldSeed":9,"simulationTick":12,"generationVersion":1,"worldWidth":100,"worldHeight":50,"chunkWidth":10,"chunkHeight":10,"characters":[],"buildings":[],"settlements":[],"households":[],"cultures":[],"factions":[],"relations":[],"politicalGroups":[],"stability":[],"militaryUnits":[],"exploration":[],"history":[],"deposits":[],"wildlife":[],"lodOverrides":[],"occupancy":[]}
            """;
        var envelope = new SaveSerializer().Deserialize(json);
        Assert.Equal(4, envelope.SaveVersion);
        var host = SimulationHost.FromSave(envelope);
        Assert.Equal(9UL, host.WorldSeed);
        Assert.Equal(12UL, host.Clock.Tick);
        Assert.Empty(host.Diplomacy.Pacts.All);
    }
}

public sealed class BetaContentTests
{
    [Fact]
    public void Generation_v2_adds_new_biomes_without_changing_v1_classifier()
    {
        Assert.Equal(BiomeId.Taiga, WorldGenerator.Classify(0.40f, false, new ClimateSample(0.35f, 0.70f)));
        Assert.Equal(BiomeId.Forest, WorldGenerator.Classify(0.50f, false, new ClimateSample(0.50f, 0.60f)));
        Assert.Equal(BiomeId.Swamp, WorldGenerator.Classify(0.40f, false, new ClimateSample(0.45f, 0.72f)));
        Assert.Equal(BiomeId.Savanna, WorldGenerator.Classify(0.50f, false, new ClimateSample(0.58f, 0.45f)));
    }

    [Fact]
    public void Debug_world_contains_new_biomes()
    {
        var host = new SimulationHost(4, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.Equal(WorldGeneration.CurrentVersion, host.World.Configuration.GenerationVersion);
        var seen = new HashSet<BiomeId>();
        for (var x = 0; x < host.World.Configuration.Width; x++)
        {
            for (var y = 0; y < host.World.Configuration.Height; y++)
                seen.Add(host.World.Grid.GetCell(new LogicalGridCoordinate(x, y)).Biome);
        }

        Assert.Contains(seen, b => b is BiomeId.Swamp or BiomeId.Savanna or BiomeId.Taiga);
    }

    [Fact]
    public void Hunt_depletes_wildlife_and_gives_food_without_war()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var person = host.Population[0];
        host.Ecology.EnsureCell(person.Position);
        var chunk = host.World.Chunks.ToAddress(person.Position).Chunk;
        Assert.True(host.Ecology.Wildlife.TryGet(chunk, out var before));
        var total = before.Total;
        var food = person.Inventory.GetQuantity(Cultures.Economy.ResourceType.Food);
        Assert.True(host.Commands.Execute(new HuntWildlifeCommand(person.Id)).Success);
        Assert.Equal(total - 1, host.Ecology.Wildlife.GetOrCreate(chunk).Total);
        Assert.Equal(food + 2, person.Inventory.GetQuantity(Cultures.Economy.ResourceType.Food));
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.WildlifeHunted);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.Equal(FactionRelationStance.Neutral, host.Diplomacy.StanceOf(a, b));
    }

    [Fact]
    public void Hunting_camp_and_fishery_exist_as_catalog_content()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.True(host.Catalog.TryGet(BuildingTypeId.HuntingCamp, out _));
        Assert.True(host.Catalog.TryGet(BuildingTypeId.Fishery, out _));
        Assert.True(host.Professions.TryGet(ProfessionId.Hunter, out _));
        Assert.True(host.Professions.TryGet(ProfessionId.Fisher, out _));
        var origin = PopulationSpawner.FindLandOrigin(host.World);
        Assert.True(host.Placement.TryPlace(BuildingTypeId.HuntingCamp, origin).Success
            || host.Buildings.All.Any(b => b.TypeId == BuildingTypeId.HuntingCamp));
    }

    [Fact]
    public void Trade_pact_is_allowed_on_neutral_alliance_needs_friendly_and_is_not_war()
    {
        var host = new SimulationHost(1, populationCount: 1, placeDevelopmentBuildings: false);
        var a = host.Civilization.Factions[0].Id;
        var b = host.Civilization.Factions[1].Id;
        Assert.True(host.Commands.Execute(new FormDiplomaticPactCommand(a, b, DiplomaticPactKind.Trade)).Success);
        Assert.False(host.Commands.Execute(new FormDiplomaticPactCommand(a, b, DiplomaticPactKind.Alliance)).Success);
        Assert.True(host.Commands.Execute(new SetDiplomaticStanceCommand(a, b, FactionRelationStance.Friendly)).Success);
        Assert.True(host.Commands.Execute(new FormDiplomaticPactCommand(a, b, DiplomaticPactKind.Alliance)).Success);
        Assert.Equal(2, host.Diplomacy.Pacts.Count);
        Assert.Equal(FactionRelationStance.Friendly, host.Diplomacy.StanceOf(a, b));
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.PactFormed);

        var envelope = host.CreateSave();
        var restored = SimulationHost.FromSave(envelope);
        Assert.Equal(2, restored.Diplomacy.Pacts.Count);
        Assert.Equal(restored.Diplomacy.Pacts.All[0].Id, host.Diplomacy.Pacts.All[0].Id);
    }

    [Fact]
    public void Season_change_is_history_not_spam()
    {
        var calendar = new SimulationCalendar
        {
            TicksPerMinute = 1,
            MinutesPerHour = 1,
            HoursPerDay = 1,
            DaysPerSeason = 2,
            SeasonsPerYear = 4
        };
        var host = new SimulationHost(1, calendar, populationCount: 1, placeDevelopmentBuildings: false);
        var before = host.History.Directory.Count;
        host.Step(1);
        Assert.Equal(before, host.History.Directory.Count);
        host.Step(2);
        Assert.Contains(host.History.Directory.All, e => e.Kind == HistoryKind.SeasonChanged);
    }
}

public sealed class ReleaseCandidateTests
{
    [Fact]
    public void Chronicle_and_help_are_presentation_safe()
    {
        var host = new SimulationHost(2, populationCount: 1, placeDevelopmentBuildings: false);
        Assert.False(string.IsNullOrWhiteSpace(HistoryChronicle.RenderRecent(host.History, 3)));
        Assert.Contains("-/=", PlayGuide.Controls, StringComparison.Ordinal);
        Assert.Contains("MMB pan", PlayGuide.Controls, StringComparison.Ordinal);
        Assert.Contains("edge scroll", PlayGuide.Controls, StringComparison.Ordinal);
        var settings = new PresentationSettings();
        settings.CycleHudFont();
        Assert.Equal(13, settings.HudFontSize);
        Assert.Equal(0.25f, settings.PlaySpeedRate);
        Assert.Equal(0.4, settings.SecondsPerTick, 5);
        settings.Faster();
        Assert.Equal(2, settings.PlaySpeed);
        Assert.Equal(0.40f, settings.PlaySpeedRate);
        settings.SetPlaySpeed(3);
        Assert.Equal(0.60f, settings.PlaySpeedRate);
        settings.Faster();
        Assert.Equal(3, settings.PlaySpeed);
        settings.Slower();
        Assert.Equal(2, settings.PlaySpeed);
    }

    [Fact]
    public void File_save_store_is_atomic()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cultures-rc-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileSaveStore(directory);
            var host = new SimulationHost(8, populationCount: 1, placeDevelopmentBuildings: false);
            host.WriteSave(store, "play");
            Assert.True(store.TryRead("play", out var json));
            Assert.Contains("\"saveVersion\": 4", json, StringComparison.Ordinal);
            var loaded = SimulationHost.LoadSave(store, "play");
            Assert.Equal(host.WorldSeed, loaded.WorldSeed);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
