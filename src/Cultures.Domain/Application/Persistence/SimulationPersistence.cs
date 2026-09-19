using Cultures.Application;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Core.Ids;
using Cultures.Economy;
using Cultures.Exploration;
using Cultures.History;
using Cultures.Military;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.Application.Persistence;

/// <summary>
/// Captures and restores authoritative simulation state. Terrain stays generated from seed.
/// </summary>
public static class SimulationPersistence
{
    public static SaveEnvelope Capture(SimulationHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        var cfg = host.World.Configuration;
        var ids = host.Ids.Snapshot();
        return new SaveEnvelope
        {
            SaveVersion = SaveEnvelope.CurrentVersion,
            WorldSeed = host.WorldSeed,
            SimulationTick = host.Clock.Tick,
            GenerationVersion = cfg.GenerationVersion,
            WorldWidth = cfg.Width,
            WorldHeight = cfg.Height,
            ChunkWidth = cfg.ChunkWidth,
            ChunkHeight = cfg.ChunkHeight,
            CursorX = host.Cursor.Position.X,
            CursorY = host.Cursor.Position.Y,
            Ids = new EntityIdCountersRecord(
                ids.NextCharacter, ids.NextFamily, ids.NextHousehold, ids.NextBuilding, ids.NextSettlement,
                ids.NextCivilization, ids.NextCulture, ids.NextFaction, ids.NextPoliticalGroup,
                ids.NextMilitaryUnit, ids.NextHistoryEvent, ids.NextResourceDeposit, ids.NextDiplomaticPact,
                ids.NextRegion, ids.NextChunk, ids.NextMigrationGroup),
            Characters = host.Population.All.Select(CaptureCharacter).ToList(),
            Buildings = host.Buildings.All.Select(CaptureBuilding).ToList(),
            Settlements = host.Settlements.All.Select(s => new SettlementSaveRecord(
                s.Id.Value, s.NameKey, s.FoundedTick, s.Culture.Value, s.Leader.Value, (byte)s.Lifecycle,
                s.Core.X, s.Core.Y, s.PresenceEvals, s.UnmatchedEvals)).ToList(),
            Households = host.Households.All.Select(h => new HouseholdSaveRecord(h.Id.Value, h.FoundedTick, h.Home.Value)).ToList(),
            Cultures = host.Civilization.Cultures.All.Select(CivilizationMapper.ToRecord).ToList(),
            Factions = host.Civilization.Factions.All.Select(CivilizationMapper.ToRecord).ToList(),
            Relations = host.Civilization.Relations.All.Select(CivilizationMapper.ToRecord).ToList(),
            PoliticalGroups = host.Politics.Groups.All.Select(CivilizationMapper.ToRecord).ToList(),
            Stability = host.Politics.Stability.Entries.Select(e => CivilizationMapper.ToRecord(e.Faction, e.Stability)).ToList(),
            MilitaryUnits = host.Military.Units.All.Select(CivilizationMapper.ToRecord).ToList(),
            Exploration = host.Exploration.Knowledge.All.Select(ExplorationKnowledgeMapper.ToRecord).ToList(),
            History = host.History.Directory.All.Select(h => new HistorySaveRecord(
                h.Id.Value, h.Tick, (byte)h.Kind, (byte)h.Importance, h.Subject, h.Secondary,
                h.LocationX, h.LocationY, h.Summary)).ToList(),
            Deposits = host.Ecology.Deposits.All.Select(d => new DepositSaveRecord(
                d.Id.Value, d.Cell.X, d.Cell.Y, (byte)d.Resource, d.Stock, d.Capacity)).ToList(),
            Wildlife = host.Ecology.Wildlife.All.Select(w => new WildlifeSaveRecord(
                w.Chunk.X, w.Chunk.Y, w.Deer, w.Sheep, w.Boar, w.Birds)).ToList(),
            LodOverrides = host.Lod.Chunks.All
                .Where(c => c.ForcedTier is not null)
                .Select(c => new LodOverrideRecord(c.Coordinate.X, c.Coordinate.Y, (byte)c.ForcedTier!.Value))
                .ToList(),
            Occupancy = host.World.Grid.OccupancyEntries
                .Where(e => e.Occupancy.Kind == OccupantKind.DebugMarker)
                .Select(e => new OccupancySaveRecord(e.Cell.X, e.Cell.Y, (byte)e.Occupancy.Kind, e.Occupancy.EntityValue, e.Occupancy.BlocksMovement))
                .ToList(),
            Pacts = host.Diplomacy.Pacts.All.Select(p => new DiplomaticPactSaveRecord(
                p.Id.Value, p.Lower.Value, p.Higher.Value, (byte)p.Kind, p.FormedTick)).ToList(),
            Speed = host.Clock.Speed,
            OnboardingComplete = host.OnboardingComplete
        };
    }

    public static void Restore(SimulationHost host, SaveEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(envelope);
        Validate(envelope, host);

        host.History.IsEnabled = false;
        try
        {
            RestoreCivilization(host, envelope);
            RestoreHouseholds(host, envelope);
            RestoreBuildings(host, envelope);
            RestoreCharacters(host, envelope);
            RestoreSettlements(host, envelope);
            RestoreExploration(host, envelope);
            RestoreHistory(host, envelope);
            RestoreEcology(host, envelope);
            RestoreOccupancy(host, envelope);
            RestoreLod(host, envelope);
            RestorePacts(host, envelope);
            if (envelope.Ids is { } counters)
            {
                host.Ids.Restore(new EntityIdCounters(
                    counters.NextCharacter, counters.NextFamily, counters.NextHousehold, counters.NextBuilding,
                    counters.NextSettlement, counters.NextCivilization, counters.NextCulture, counters.NextFaction,
                    counters.NextPoliticalGroup, counters.NextMilitaryUnit, counters.NextHistoryEvent,
                    counters.NextResourceDeposit, counters.NextDiplomaticPact, counters.NextRegion, counters.NextChunk,
                    counters.NextMigrationGroup));
            }
        }
        finally
        {
            host.History.IsEnabled = true;
        }

        host.Lod.Evaluate();
        host.Clock.SetSpeed(envelope.Speed < 1 ? 1 : envelope.Speed);
        host.OnboardingComplete = envelope.OnboardingComplete;
        host.WorldEvents.RestoreSeason(host.Clock.Date.SeasonIndex);
    }

    private static void Validate(SaveEnvelope envelope, SimulationHost host)
    {
        var seenCharacters = new HashSet<ulong>();
        foreach (var person in envelope.Characters ?? [])
        {
            if (!seenCharacters.Add(person.Id))
                throw new InvalidOperationException($"Duplicate character {person.Id}.");
            if (person.Inventory.Any(i => i.Quantity < 0))
                throw new InvalidOperationException($"Invalid inventory on character {person.Id}.");
        }

        var seenBuildings = new HashSet<ulong>();
        foreach (var building in envelope.Buildings ?? [])
        {
            if (!seenBuildings.Add(building.Id))
                throw new InvalidOperationException($"Duplicate building {building.Id}.");
            if (!host.Catalog.TryGet(new BuildingTypeId(building.TypeId), out _))
                throw new InvalidOperationException($"Unknown building type '{building.TypeId}'.");
        }

        foreach (var person in envelope.Characters ?? [])
        {
            if (person.Faction != 0 && envelope.Factions?.All(f => f.Id != person.Faction) == true)
                throw new InvalidOperationException($"Character {person.Id} references missing faction.");
            if (person.MilitaryUnit != 0)
            {
                var unit = envelope.MilitaryUnits?.FirstOrDefault(u => u.Id == person.MilitaryUnit);
                if (unit is null)
                    throw new InvalidOperationException($"Character {person.Id} references missing military unit.");
                if (person.Faction != 0 && unit.Faction != person.Faction)
                    throw new InvalidOperationException($"Cross-faction military membership on {person.Id}.");
            }

            if (person.PoliticalGroup != 0)
            {
                var group = envelope.PoliticalGroups?.FirstOrDefault(g => g.Id == person.PoliticalGroup);
                if (group is null)
                    throw new InvalidOperationException($"Character {person.Id} references missing political group.");
                if (person.Faction != 0 && group.Faction != person.Faction)
                    throw new InvalidOperationException($"Cross-faction political membership on {person.Id}.");
            }
        }
    }

    private static CharacterSaveRecord CaptureCharacter(CharacterState person)
    {
        var target = person.Activity.Target;
        return new CharacterSaveRecord(
            person.Id.Value,
            person.Name,
            person.AppearanceSeed,
            person.AgeYears,
            (byte)person.LifeStage,
            person.Position.X,
            person.Position.Y,
            person.Needs.Hunger,
            person.Needs.Fatigue,
            person.Health.Current,
            person.Health.Max,
            InventorySave.ToEntries(person.Inventory),
            person.Skills.Enumerate().Where(s => s.Experience > 0)
                .Select(s => new SkillSaveRecord((byte)s.Type, s.Experience)).ToList(),
            person.FamilyLinks.Parents.Select(p => p.Value).ToList(),
            person.FamilyLinks.Children.Select(p => p.Value).ToList(),
            person.FamilyLinks.Caregivers.Select(p => p.Value).ToList(),
            (byte)person.Activity.Kind,
            person.Activity.DurationTicks,
            person.Activity.ProgressTicks,
            target?.X,
            target?.Y,
            person.Activity.PartnerId.Value,
            person.Activity.Skill is { } skill ? (byte)skill : null,
            person.Activity.RemainingPath.Select(p => new GridPointRecord(p.X, p.Y)).ToList(),
            person.AssignedWorkplace.Building.Value,
            person.AssignedWorkplace.Slot,
            person.Family.Value,
            person.Household.Value,
            person.Settlement.Value,
            person.Culture.Value,
            person.Faction.Value,
            person.PoliticalGroup.Value,
            person.MilitaryUnit.Value,
            person.Profession.Value,
            person.Partner.Value,
            (byte)person.LodTier,
            person.IsPersistentIndividual,
            person.IsPlayerCommanded);
    }

    private static BuildingSaveRecord CaptureBuilding(BuildingState building) => new(
        building.Id.Value,
        building.TypeId.Value,
        building.Origin.X,
        building.Origin.Y,
        (byte)building.Lifecycle,
        building.AssociatedSettlement.Value,
        building.Production.CurrentRecipe?.Value,
        building.Production.ProgressTicks,
        building.Production.DurationTicks,
        InventorySave.ToEntries(building.Inventory),
        building.Workplaces.Select(w => w.Worker.Value).ToList());

    private static void RestoreCivilization(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Cultures ?? [])
        {
            if (record.Id == CultureId.Neutral.Value)
                continue;
            host.Civilization.Cultures.Add(CivilizationMapper.FromRecord(record));
        }

        foreach (var record in envelope.Factions ?? [])
            host.Civilization.Factions.Add(CivilizationMapper.FromRecord(record));

        foreach (var record in envelope.Relations ?? [])
        {
            var relation = CivilizationMapper.FromRecord(record);
            var stored = host.Civilization.Relations.GetOrCreate(relation.Lower, relation.Higher);
            stored.Stance = relation.Stance;
        }

        foreach (var record in envelope.PoliticalGroups ?? [])
            host.Politics.Groups.Add(CivilizationMapper.FromRecord(record));

        foreach (var record in envelope.Stability ?? [])
            host.Politics.Stability.Set(new FactionId(record.Faction), CivilizationMapper.FromRecord(record));

        foreach (var record in envelope.MilitaryUnits ?? [])
            host.Military.Units.Add(CivilizationMapper.FromRecord(record));
    }

    private static void RestoreHouseholds(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Households ?? [])
        {
            host.Households.Add(new HouseholdState(new HouseholdId(record.Id), record.FoundedTick)
            {
                Home = new BuildingId(record.Home)
            });
        }
    }

    private static void RestoreBuildings(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Buildings ?? [])
        {
            var definition = host.Catalog.Get(new BuildingTypeId(record.TypeId));
            var origin = new LogicalGridCoordinate(record.OriginX, record.OriginY);
            if (!definition.Footprint.TryMaterialize(host.World.Topology, origin, out var cells))
                throw new InvalidOperationException($"Building {record.Id} footprint is outside the world.");
            var access = cells[0];
            foreach (var cell in cells)
            {
                foreach (var (dx, dy) in HexGrid.NeighborOffsets(cell.Y))
                {
                    var resolution = host.World.Topology.Resolve(cell.X + dx, cell.Y + dy);
                    if (resolution.TryGetCell(out var neighbor) && host.World.Grid.GetCell(neighbor).Passable)
                    {
                        access = neighbor;
                        break;
                    }
                }
            }

            var workplaces = new WorkplaceSlot[definition.WorkplaceCount];
            for (var i = 0; i < workplaces.Length; i++)
                workplaces[i] = new WorkplaceSlot(i, access);

            var building = new BuildingState(new BuildingId(record.Id), definition, origin, cells, workplaces, access)
            {
                Lifecycle = (BuildingLifecycle)record.Lifecycle,
                AssociatedSettlement = new SettlementId(record.Settlement)
            };
            InventorySave.Fill(building.Inventory, record.Inventory);
            RecipeId? recipe = string.IsNullOrEmpty(record.Recipe) ? null : new RecipeId(record.Recipe);
            building.Production.Restore(recipe, record.ProductionProgress, record.ProductionDuration);
            host.Placement.Restore(building);
        }
    }

    private static void RestoreCharacters(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Characters ?? [])
        {
            var person = new CharacterState(
                new CharacterId(record.Id),
                new LogicalGridCoordinate(record.X, record.Y),
                record.AppearanceSeed)
            {
                Name = record.Name,
                AgeYears = record.AgeYears,
                LifeStage = (CharacterLifeStage)record.LifeStage,
                Family = new FamilyId(record.Family),
                Household = new HouseholdId(record.Household),
                Settlement = new SettlementId(record.Settlement),
                Culture = new CultureId(record.Culture),
                Faction = new FactionId(record.Faction),
                PoliticalGroup = new PoliticalGroupId(record.PoliticalGroup),
                MilitaryUnit = new MilitaryUnitId(record.MilitaryUnit),
                Profession = new ProfessionId(record.Profession ?? ""),
                Partner = new CharacterId(record.Partner),
                LodTier = (SimulationLodTier)record.LodTier,
                IsPersistentIndividual = record.PersistentIndividual,
                IsPlayerCommanded = record.PlayerCommanded
            };
            person.Needs.Hunger = record.Hunger;
            person.Needs.Fatigue = record.Fatigue;
            person.Health.Current = record.Health;
            person.Health.Max = record.HealthMax;
            InventorySave.Fill(person.Inventory, record.Inventory);
            foreach (var skill in record.Skills ?? [])
                person.Skills.SetExperience((SkillType)skill.Skill, skill.Experience);
            person.FamilyLinks.Restore(
                (record.Parents ?? []).Select(id => new CharacterId(id)),
                (record.Children ?? []).Select(id => new CharacterId(id)),
                (record.Caregivers ?? []).Select(id => new CharacterId(id)));
            LogicalGridCoordinate? target = record.ActionTargetX is { } tx && record.ActionTargetY is { } ty
                ? new LogicalGridCoordinate(tx, ty)
                : null;
            person.Activity.Restore(
                (ActionKind)record.ActionKind,
                record.ActionDuration,
                record.ActionProgress,
                target,
                new CharacterId(record.ActionPartner),
                record.ActionSkill is { } skillType ? (SkillType)skillType : null,
                (record.Path ?? []).Select(p => new LogicalGridCoordinate(p.X, p.Y)));
            if (record.WorkplaceBuilding != 0)
                person.AssignedWorkplace = new WorkplaceId(new BuildingId(record.WorkplaceBuilding), record.WorkplaceSlot);
            host.Population.Add(person);
        }

        foreach (var record in envelope.Buildings ?? [])
        {
            if (!host.Buildings.TryGet(new BuildingId(record.Id), out var building))
                continue;
            for (var i = 0; i < building.Workplaces.Count && i < (record.Workers?.Count ?? 0); i++)
                building.Workplaces[i].Worker = new CharacterId(record.Workers![i]);
        }
    }

    private static void RestoreSettlements(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Settlements ?? [])
        {
            var settlement = new SettlementState(new SettlementId(record.Id), record.NameKey, record.FoundedTick)
            {
                Culture = new CultureId(record.Culture),
                Leader = new CharacterId(record.Leader),
                Lifecycle = (SettlementLifecycle)record.Lifecycle,
                Core = new LogicalGridCoordinate(record.CoreX, record.CoreY),
                PresenceEvals = record.PresenceEvals,
                UnmatchedEvals = record.UnmatchedEvals
            };
            host.Settlements.Add(settlement);
        }
    }

    private static void RestoreExploration(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Exploration ?? [])
        {
            var knowledge = ExplorationKnowledgeMapper.FromRecord(record);
            var stored = host.Exploration.Knowledge.GetOrCreate(knowledge.Coordinate);
            stored.Level = knowledge.Level;
            stored.Source = knowledge.Source;
            stored.DiscoveredTick = knowledge.DiscoveredTick;
            stored.UpdatedTick = knowledge.UpdatedTick;
            stored.Terrain = knowledge.Terrain;
            stored.Biome = knowledge.Biome;
            stored.Climate = knowledge.Climate;
            stored.PersistentChunk = knowledge.PersistentChunk;
        }
    }

    private static void RestoreHistory(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.History ?? [])
        {
            host.History.Directory.Add(new HistoryRecord(
                new HistoryEventId(record.Id),
                record.Tick,
                (HistoryKind)record.Kind,
                (HistoryImportance)record.Importance,
                record.Subject,
                record.Secondary,
                record.X,
                record.Y,
                record.Summary));
        }
    }

    private static void RestoreEcology(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Deposits ?? [])
        {
            host.Ecology.Deposits.Add(new ResourceDeposit(
                new ResourceDepositId(record.Id),
                new LogicalGridCoordinate(record.X, record.Y),
                (ResourceType)record.Resource,
                record.Stock,
                record.Capacity));
        }

        foreach (var record in envelope.Wildlife ?? [])
        {
            var wildlife = host.Ecology.Wildlife.GetOrCreate(new ChunkCoordinate(record.ChunkX, record.ChunkY));
            wildlife.Deer = record.Deer;
            wildlife.Sheep = record.Sheep;
            wildlife.Boar = record.Boar;
            wildlife.Birds = record.Birds;
        }
    }

    private static void RestoreOccupancy(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Occupancy ?? [])
        {
            host.World.Grid.TrySetOccupancy(
                new WorldCoordinate(record.X, record.Y),
                new Occupancy((OccupantKind)record.Kind, record.Entity, record.Blocks));
        }
    }

    private static void RestoreLod(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.LodOverrides ?? [])
        {
            var state = host.Lod.Chunks.GetOrCreate(new ChunkCoordinate(record.ChunkX, record.ChunkY));
            state.ForcedTier = (SimulationLodTier)record.Tier;
        }
    }

    private static void RestorePacts(SimulationHost host, SaveEnvelope envelope)
    {
        foreach (var record in envelope.Pacts ?? [])
        {
            host.Diplomacy.Pacts.Add(new DiplomaticPact(
                new DiplomaticPactId(record.Id),
                new FactionId(record.Lower),
                new FactionId(record.Higher),
                (DiplomaticPactKind)record.Kind,
                record.FormedTick));
        }
    }
}
