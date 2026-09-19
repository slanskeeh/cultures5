using Cultures.Application;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Economy;
using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.Military;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;
using Cultures.World.Commands;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Application shell: clock, world cursor, character and building debug inspect.
/// Domain state is not stored on this Node.
/// </summary>
public partial class Main : Control
{
    public const double SecondsPerTick = 0.1;
    public const int DebugFontSize = 11;

    private SimulationHost _host = null!;
    private ColorRect _hud = null!;
    private Label _label = null!;
    private WorldDebugMap _map = null!;
    private double _accumulator;
    private string _lastCommand = "ready";
    private int _selectedIndex;
    private int _selectedBuildingIndex;
    private int _selectedSettlementIndex;
    private int _selectedFactionIndex;
    private int _selectedGroupIndex;
    private int _selectedUnitIndex;

    public override void _Ready()
    {
        _host = new SimulationHost(worldSeed: 1, world: WorldConfiguration.DebugSample);
        _hud = GetNode<ColorRect>("Hud");
        _label = GetNode<Label>("Hud/DebugLabel");
        _label.AddThemeFontSizeOverride("font_size", DebugFontSize);
        _label.VerticalAlignment = VerticalAlignment.Top;
        _map = GetNode<WorldDebugMap>("WorldDebugMap");
        _map.Host = _host;
        SnapToSelected();
        ProtectSelected();
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            return;

        switch (key.Keycode)
        {
            case Key.Space:
                _host.Clock.SetPaused(!_host.Clock.IsPaused);
                _lastCommand = _host.Clock.IsPaused ? "clock paused" : "clock resumed";
                break;
            case Key.Left:
                Move(-1, 0);
                break;
            case Key.Right:
                Move(1, 0);
                break;
            case Key.Up:
                Move(0, -1);
                break;
            case Key.Down:
                Move(0, 1);
                break;
            case Key.G:
                ToggleOccupancy();
                break;
            case Key.Tab:
                CycleSelection();
                break;
            case Key.C:
                SnapToSelected();
                break;
            case Key.B:
                CycleBuilding();
                break;
            case Key.V:
                SnapToBuilding();
                break;
            case Key.N:
                DebugCreateChild();
                break;
            case Key.T:
                DebugTeach();
                break;
            case Key.K:
                DebugGrantSkill();
                break;
            case Key.M:
                CycleSettlement();
                break;
            case Key.U:
                SnapToSettlement();
                break;
            case Key.E:
                DebugEvaluateSettlements();
                break;
            case Key.L:
                _map.ShowLod = !_map.ShowLod;
                _lastCommand = _map.ShowLod ? "lod overlay on" : "lod overlay off";
                break;
            case Key.O:
                DebugRefreshLod();
                break;
            case Key.Key9:
                DebugForceLod(SimulationLodTier.Aggregate);
                break;
            case Key.Key0:
                DebugForceLod(SimulationLodTier.Full);
                break;
            case Key.R:
                DebugExplore(ExplorationKnowledgeLevel.Rumored);
                break;
            case Key.S:
                DebugExplore(ExplorationKnowledgeLevel.Scouted);
                break;
            case Key.D:
                DebugExplore(ExplorationKnowledgeLevel.Mapped);
                break;
            case Key.F:
                DebugExplore(ExplorationKnowledgeLevel.Confirmed);
                break;
            case Key.A:
                DebugExplore(ExplorationKnowledgeLevel.Analyzed);
                break;
            case Key.Q:
                _map.ShowExploration = !_map.ShowExploration;
                _lastCommand = _map.ShowExploration ? "exploration overlay on" : "exploration overlay off";
                break;
            case Key.P:
                CycleFaction();
                break;
            case Key.J:
                DebugJoinFaction();
                break;
            case Key.H:
                DebugCycleRelation();
                break;
            case Key.I:
                CyclePoliticalGroup();
                break;
            case Key.Y:
                DebugJoinPoliticalGroup();
                break;
            case Key.W:
                DebugCycleStability();
                break;
            case Key.Key1:
                DebugAdjustInfluence(-10);
                break;
            case Key.Key2:
                DebugAdjustInfluence(10);
                break;
            case Key.X:
                CycleMilitaryUnit();
                break;
            case Key.Z:
                DebugJoinMilitaryUnit();
                break;
            case Key.Key3:
                DebugDisbandMilitaryUnit();
                break;
            default:
                return;
        }

        Refresh();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!_host.Clock.IsPaused)
        {
            _accumulator += delta;
            while (_accumulator >= SecondsPerTick)
            {
                _accumulator -= SecondsPerTick;
                _host.Step(1);
            }
        }

        Refresh();
    }

    private void Move(int dx, int dy)
    {
        var result = _host.Commands.Execute(new MoveDebugCursorCommand(dx, dy));
        _lastCommand = result.Success
            ? $"moved ({dx},{dy})"
            : result.Error ?? "move failed";
    }

    private void ToggleOccupancy()
    {
        var position = _host.Cursor.Position.ToWorld();
        _host.World.Grid.TryGetOccupancy(position, out var current);
        if (current.Kind == OccupantKind.Building)
        {
            _lastCommand = "cell occupied by building";
            return;
        }

        var next = current.IsOccupied ? Occupancy.Empty : Occupancy.DebugMarker;
        var result = _host.Commands.Execute(new SetOccupancyCommand(position, next));
        _lastCommand = result.Success
            ? (next.IsOccupied ? "marked occupied" : "cleared occupancy")
            : result.Error ?? "occupancy failed";
    }

    private void CycleSelection()
    {
        if (_host.Population.Count == 0)
            return;
        var previous = _host.Population[_selectedIndex];
        _selectedIndex = (_selectedIndex + 1) % _host.Population.Count;
        if (previous.IsPersistentIndividual)
            _host.Commands.Execute(new ProtectCharacterCommand(previous.Id, false));
        ProtectSelected();
        _lastCommand = $"selected {_host.Population[_selectedIndex].Id}";
    }

    private void CycleBuilding()
    {
        if (_host.Buildings.Count == 0)
            return;
        _selectedBuildingIndex = (_selectedBuildingIndex + 1) % _host.Buildings.Count;
        _lastCommand = $"selected {_host.Buildings[_selectedBuildingIndex].Id}";
    }

    private void SnapToSelected()
    {
        if (_host.Population.Count == 0)
            return;
        var character = _host.Population[_selectedIndex];
        var dx = _host.World.Topology.SignedHorizontalDelta(_host.Cursor.Position.X, character.Position.X);
        var dy = character.Position.Y - _host.Cursor.Position.Y;
        var result = _host.Commands.Execute(new MoveDebugCursorCommand(dx, dy));
        _lastCommand = result.Success ? $"cursor to {character.Id}" : result.Error ?? "snap failed";
    }

    private void DebugCreateChild()
    {
        if (_host.Population.Count < 1)
            return;
        var parentA = _host.Population[_selectedIndex];
        var parentB = _host.Population[(_selectedIndex + 1) % _host.Population.Count];
        var result = _host.Commands.Execute(new CreateChildCommand(parentA.Id, parentB.Id));
        _lastCommand = result.Success ? $"child of {parentA.Id}+{parentB.Id}" : result.Error ?? "birth failed";
    }

    private void DebugTeach()
    {
        if (_host.Population.Count < 1)
            return;
        var teacher = _host.Population[_selectedIndex];
        var studentId = teacher.FamilyLinks.Children.FirstOrDefault();
        if (!studentId.IsAssigned)
            studentId = _host.Population[(_selectedIndex + 1) % _host.Population.Count].Id;
        var result = _host.Commands.Execute(new TeachCharacterCommand(teacher.Id, studentId, SkillType.Farming));
        _lastCommand = result.Success ? $"teach farming {teacher.Id}→{studentId}" : result.Error ?? "teach failed";
    }

    private void DebugGrantSkill()
    {
        if (_host.Population.Count < 1)
            return;
        var character = _host.Population[_selectedIndex];
        var result = _host.Commands.Execute(new AddSkillExperienceCommand(character.Id, SkillType.Farming, SkillRules.XpPerLevel));
        _lastCommand = result.Success
            ? $"farming xp {character.Id} now {character.Skills.GetLevel(SkillType.Farming)}"
            : result.Error ?? "skill failed";
    }

    private void CycleSettlement()
    {
        if (_host.Settlements.Count == 0)
            return;
        _selectedSettlementIndex = (_selectedSettlementIndex + 1) % _host.Settlements.Count;
        _lastCommand = $"selected {_host.Settlements[_selectedSettlementIndex].Id}";
    }

    private void SnapToSettlement()
    {
        if (_host.Settlements.Count == 0)
            return;
        var settlement = _host.Settlements[_selectedSettlementIndex];
        var dx = _host.World.Topology.SignedHorizontalDelta(_host.Cursor.Position.X, settlement.Core.X);
        var dy = settlement.Core.Y - _host.Cursor.Position.Y;
        var result = _host.Commands.Execute(new MoveDebugCursorCommand(dx, dy));
        _lastCommand = result.Success ? $"cursor to {settlement.Id} core" : result.Error ?? "snap failed";
    }

    private void ProtectSelected()
    {
        if (_host.Population.Count == 0)
            return;
        var character = _host.Population[_selectedIndex];
        _host.Commands.Execute(new ProtectCharacterCommand(character.Id, true));
    }

    private void DebugRefreshLod()
    {
        var result = _host.Commands.Execute(new RefreshLodCommand());
        _lastCommand = result.Success ? "lod evaluated" : result.Error ?? "lod failed";
    }

    private void DebugForceLod(SimulationLodTier tier)
    {
        _host.World.Chunks.TryResolve(_host.Cursor.Position.ToWorld(), out var address);
        var result = _host.Commands.Execute(new ForceChunkLodCommand(address.Chunk, tier));
        _lastCommand = result.Success ? $"chunk {address.Chunk} → {tier}" : result.Error ?? "lod force failed";
    }

    private void DebugExplore(ExplorationKnowledgeLevel target)
    {
        if (!_host.World.Chunks.TryResolve(_host.Cursor.Position.ToWorld(), out var address))
        {
            _lastCommand = "cursor is outside the world";
            return;
        }

        ICommand command = target switch
        {
            ExplorationKnowledgeLevel.Rumored => new RumorChunkCommand(address.Chunk),
            ExplorationKnowledgeLevel.Scouted => new ScoutChunkCommand(address.Chunk),
            ExplorationKnowledgeLevel.Mapped => new MapChunkCommand(address.Chunk),
            ExplorationKnowledgeLevel.Confirmed => new ConfirmChunkCommand(address.Chunk),
            ExplorationKnowledgeLevel.Analyzed => new AnalyzeChunkCommand(address.Chunk),
            _ => new ScoutChunkCommand(address.Chunk)
        };
        var result = _host.Commands.Execute(command);
        _lastCommand = result.Success
            ? $"{address.Chunk} {target}"
            : result.Error ?? "explore failed";
    }

    private void CycleFaction()
    {
        if (_host.Civilization.Factions.Count == 0)
            return;
        _selectedFactionIndex = (_selectedFactionIndex + 1) % _host.Civilization.Factions.Count;
        _selectedGroupIndex = 0;
        var faction = _host.Civilization.Factions[_selectedFactionIndex];
        _lastCommand = $"inspect {faction.Id} {faction.Name}";
    }

    private void DebugJoinFaction()
    {
        if (_host.Population.Count == 0 || _host.Civilization.Factions.Count == 0)
            return;
        var person = _host.Population[_selectedIndex];
        var faction = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        var target = person.Faction == faction.Id ? FactionId.None : faction.Id;
        var result = _host.Commands.Execute(new AssignFactionMembershipCommand(person.Id, target));
        _lastCommand = result.Success
            ? $"{person.Id} faction {target}"
            : result.Error ?? "membership failed";
    }

    private void DebugCycleRelation()
    {
        if (_host.Civilization.Factions.Count < 2)
            return;
        var left = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        var right = _host.Civilization.Factions[(_selectedFactionIndex + 1) % _host.Civilization.Factions.Count];
        var current = _host.Diplomacy.StanceOf(left.Id, right.Id);
        var next = current switch
        {
            FactionRelationStance.Neutral => FactionRelationStance.Friendly,
            FactionRelationStance.Friendly => FactionRelationStance.Hostile,
            _ => FactionRelationStance.Neutral
        };
        var result = _host.Commands.Execute(new SetDiplomaticStanceCommand(left.Id, right.Id, next));
        _lastCommand = result.Success
            ? $"{left.Name} ↔ {right.Name} {next}"
            : result.Error ?? "diplomacy failed";
    }

    private IReadOnlyList<PoliticalGroupState> GroupsOfSelectedFaction()
    {
        if (_host.Civilization.Factions.Count == 0)
            return [];
        var faction = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        return _host.Politics.Groups.ForFaction(faction.Id);
    }

    private void CyclePoliticalGroup()
    {
        var groups = GroupsOfSelectedFaction();
        if (groups.Count == 0)
            return;
        _selectedGroupIndex = (_selectedGroupIndex + 1) % groups.Count;
        var group = groups[_selectedGroupIndex];
        _lastCommand = $"inspect {group.Id} {group.Name}";
    }

    private void DebugJoinPoliticalGroup()
    {
        if (_host.Population.Count == 0)
            return;
        var groups = GroupsOfSelectedFaction();
        if (groups.Count == 0)
            return;
        var person = _host.Population[_selectedIndex];
        var group = groups[_selectedGroupIndex % groups.Count];
        var target = person.PoliticalGroup == group.Id ? PoliticalGroupId.None : group.Id;
        var result = _host.Commands.Execute(new AssignPoliticalGroupCommand(person.Id, target));
        _lastCommand = result.Success
            ? $"{person.Id} group {target}"
            : result.Error ?? "political affiliation failed";
    }

    private void DebugCycleStability()
    {
        if (_host.Civilization.Factions.Count == 0)
            return;
        var faction = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        var current = _host.Politics.Stability.Of(faction.Id).Value;
        var next = current switch
        {
            <= PoliticsRules.UnstableCeiling => 50,
            <= PoliticsRules.TenseCeiling => 100,
            _ => 0
        };
        var result = _host.Commands.Execute(new SetInternalStabilityCommand(faction.Id, next));
        _lastCommand = result.Success
            ? $"{faction.Name} stability {next}"
            : result.Error ?? "stability failed";
    }

    private void DebugAdjustInfluence(int delta)
    {
        var groups = GroupsOfSelectedFaction();
        if (groups.Count == 0)
            return;
        var group = groups[_selectedGroupIndex % groups.Count];
        var target = group.Influence + delta;
        var result = _host.Commands.Execute(new SetPoliticalGroupInfluenceCommand(group.Id, target));
        _lastCommand = result.Success
            ? $"{group.Name} influence {target}"
            : result.Error ?? "influence failed";
    }

    private IReadOnlyList<MilitaryUnitState> UnitsOfSelectedFaction()
    {
        if (_host.Civilization.Factions.Count == 0)
            return [];
        var faction = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        return _host.Military.Units.ForFaction(faction.Id);
    }

    private void CycleMilitaryUnit()
    {
        var units = UnitsOfSelectedFaction();
        if (units.Count == 0)
            return;
        _selectedUnitIndex = (_selectedUnitIndex + 1) % units.Count;
        var unit = units[_selectedUnitIndex];
        _lastCommand = $"inspect {unit.Id} {unit.Name}";
    }

    private void DebugJoinMilitaryUnit()
    {
        if (_host.Population.Count == 0)
            return;
        var units = UnitsOfSelectedFaction();
        if (units.Count == 0)
            return;
        var person = _host.Population[_selectedIndex];
        var unit = units[_selectedUnitIndex % units.Count];
        var target = person.MilitaryUnit == unit.Id ? MilitaryUnitId.None : unit.Id;
        var result = _host.Commands.Execute(new AssignCharacterToMilitaryUnitCommand(person.Id, target));
        _lastCommand = result.Success
            ? $"{person.Id} unit {target}"
            : result.Error ?? "military affiliation failed";
    }

    private void DebugDisbandMilitaryUnit()
    {
        var units = UnitsOfSelectedFaction();
        if (units.Count == 0)
            return;
        var unit = units[_selectedUnitIndex % units.Count];
        var result = _host.Commands.Execute(new DisbandMilitaryUnitCommand(unit.Id));
        _lastCommand = result.Success
            ? $"disbanded {unit.Id} {unit.Name}"
            : result.Error ?? "disband failed";
    }

    private void DebugEvaluateSettlements()
    {
        var result = _host.Commands.Execute(new EvaluateSettlementsCommand());
        _lastCommand = result.Success
            ? $"evaluated settlements ({_host.Settlements.Count})"
            : result.Error ?? "evaluate failed";
    }

    private void SnapToBuilding()
    {
        if (_host.Buildings.Count == 0)
            return;
        var building = _host.Buildings[_selectedBuildingIndex];
        var dx = _host.World.Topology.SignedHorizontalDelta(_host.Cursor.Position.X, building.Origin.X);
        var dy = building.Origin.Y - _host.Cursor.Position.Y;
        var result = _host.Commands.Execute(new MoveDebugCursorCommand(dx, dy));
        _lastCommand = result.Success ? $"cursor to {building.Id}" : result.Error ?? "snap failed";
    }

    private void Refresh()
    {
        var date = _host.Clock.Date;
        var cursor = _host.Cursor.Position;
        _host.World.Chunks.TryResolve(cursor.ToWorld(), out var chunk);
        _host.World.Grid.TryGetCell(cursor.ToWorld(), out var terrain);
        var paused = _host.Clock.IsPaused ? "PAUSED" : "RUNNING";
        var water = terrain.IsWater ? "water" : "land";
        var selected = _host.Population.Count > 0 ? _host.Population[_selectedIndex] : null;
        var selectedBuilding = _host.Buildings.Count > 0 ? _host.Buildings[_selectedBuildingIndex] : null;
        var selectedSettlement = _host.Settlements.Count > 0
            ? _host.Settlements[_selectedSettlementIndex % _host.Settlements.Count]
            : null;
        var cursorBuilding = _host.Buildings.FindAt(cursor);
        var inspectBuilding = cursorBuilding ?? selectedBuilding;
        _map.SelectedId = selected?.Id;
        _map.SelectedBuildingId = inspectBuilding?.Id;
        _map.SelectedSettlementId = selectedSettlement?.Id;

        var workplace = selected is { AssignedWorkplace.IsAssigned: true }
            ? selected.AssignedWorkplace.ToString()
            : "none";
        var characterLine = selected is null
            ? "no population"
            : $"{selected.Id} {selected.LifeStage} age {selected.AgeYears:0.0}  {selected.Position}  " +
              $"hunger {selected.Needs.Hunger:0.00}  fatigue {selected.Needs.Fatigue:0.00}  " +
              $"food {selected.Inventory.GetQuantity(ResourceType.Food)}  {selected.Activity.Kind}  " +
              $"work {workplace}  {selected.Settlement}  {selected.Culture}  {selected.Faction}  {selected.PoliticalGroup}  {selected.MilitaryUnit}  lod {selected.LodTier}";
        var familyLine = selected is null
            ? ""
            : $"parents {string.Join(",", selected.FamilyLinks.Parents.Select(id => id.Value.ToString()))}  " +
              $"care {string.Join(",", selected.FamilyLinks.Caregivers.Select(id => id.Value.ToString()))}  " +
              $"children {string.Join(",", selected.FamilyLinks.Children.Select(id => id.Value.ToString()))}  " +
              $"partner {selected.Activity.PartnerId}  skill {selected.Activity.Skill?.ToString() ?? "-"}";
        var skillLine = selected is null
            ? ""
            : $"farm {selected.Skills.GetLevel(SkillType.Farming)}  " +
              $"wood {selected.Skills.GetLevel(SkillType.Woodworking)}  " +
              $"stone {selected.Skills.GetLevel(SkillType.Stoneworking)}  " +
              $"craft {selected.Skills.GetLevel(SkillType.Crafting)}";

        var buildingLine = inspectBuilding is null
            ? "no building"
            : $"{inspectBuilding.Id} {inspectBuilding.TypeId} {inspectBuilding.Lifecycle} {inspectBuilding.Origin}  " +
              $"food {inspectBuilding.Inventory.GetQuantity(ResourceType.Food)}  " +
              $"wood {inspectBuilding.Inventory.GetQuantity(ResourceType.Wood)}  " +
              $"recipe {inspectBuilding.Production.CurrentRecipe?.Value ?? inspectBuilding.Definition.Recipe?.Value ?? "-"}  " +
              $"prod {inspectBuilding.Production.ProgressTicks}/{inspectBuilding.Production.DurationTicks}  " +
              $"workers {string.Join(",", inspectBuilding.Workplaces.Select(w => w.Worker.IsAssigned ? w.Worker.Value.ToString() : "-"))}  " +
              $"{inspectBuilding.AssociatedSettlement}";

        var stats = selectedSettlement?.Statistics;
        var settlementLine = selectedSettlement is null
            ? "no settlement"
            : $"{selectedSettlement.Id} {selectedSettlement.Lifecycle} {selectedSettlement.NameKey}  " +
              $"core {selectedSettlement.Core}  pop {stats!.Population}  " +
              $"inf {stats.Infants} ch {stats.Children} adol {stats.Adolescents} ad {stats.Adults} el {stats.Elders}  " +
              $"bld {stats.ActiveBuildings} sh {stats.Shelters} st {stats.StorageBuildings}  " +
              $"food {stats.FoodStored} work {stats.Workers}/{stats.UnemployedAdults} prod {stats.EstimatedFoodProduction}";

        var totals = _host.Lod.ResourceTotals();
        var lodTier = _host.Lod.Classify(cursor);
        _host.Lod.Chunks.TryGet(chunk.Chunk, out var chunkState);
        var census = chunkState?.Census;
        var lodLine =
            $"lod {lodTier}  pres {chunkState?.Presentation.ToString() ?? "Unloaded"}  " +
            $"pop {census?.Population ?? 0} bld {census?.Buildings ?? 0}  " +
            $"food {census?.Food ?? 0}/{totals.Food} wood {totals.Wood} stone {totals.Stone}  " +
            $"mig {census?.MigrationPressure ?? 0}";

        var knowledge = _host.Exploration.GetKnowledge(chunk.Chunk);
        var facts = _host.Exploration.GetKnownFacts(chunk.Chunk);
        var known = facts.Level == ExplorationKnowledgeLevel.Unknown
            ? "none"
            : $"land {facts.Terrain.HasLand} water {facts.Terrain.HasWater}" +
              (facts.Biome.Known ? $" biome {facts.Biome.Dominant}" : "") +
              (facts.Climate.Known ? $" t {facts.Climate.Temperature:0.00}" : "");
        var explorationLine =
            $"explore {knowledge.Level}  {chunk.Chunk}  facts {known}  known-chunks {_host.Exploration.Knowledge.Count}";

        var selectedFaction = _host.Civilization.Factions.Count > 0
            ? _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count]
            : null;
        var otherFaction = _host.Civilization.Factions.Count > 1
            ? _host.Civilization.Factions[(_selectedFactionIndex + 1) % _host.Civilization.Factions.Count]
            : null;
        _host.Civilization.Cultures.TryGet(selectedFaction?.Culture ?? CultureId.Neutral, out var factionCulture);
        var factionLine = selectedFaction is null
            ? $"cultures {_host.Civilization.Cultures.Count}  no factions"
            : $"cultures {_host.Civilization.Cultures.Count}  factions {_host.Civilization.Factions.Count}  " +
              $"{selectedFaction.Id} {selectedFaction.Name}  cult {factionCulture?.Name ?? selectedFaction.Culture.ToString()}  " +
              $"members {_host.Civilization.CountMembers(selectedFaction.Id)}  home {selectedFaction.HomeSettlement}";
        var relationLine = selectedFaction is null || otherFaction is null
            ? "no relation"
            : $"{selectedFaction.Name} ↔ {otherFaction.Name}  {_host.Diplomacy.StanceOf(selectedFaction.Id, otherFaction.Id)}";
        var groups = selectedFaction is null ? [] : _host.Politics.Groups.ForFaction(selectedFaction.Id);
        var selectedGroup = groups.Count > 0 ? groups[_selectedGroupIndex % groups.Count] : null;
        var stability = selectedFaction is null
            ? InternalStability.Default
            : _host.Politics.Stability.Of(selectedFaction.Id);
        var politicsLine = selectedFaction is null
            ? "no politics"
            : $"stab {stability.Value} {stability.Band}  groups {groups.Count}";
        var groupLine = selectedGroup is null
            ? "no political group"
            : $"{selectedGroup.Id} {selectedGroup.Name}  members {_host.Politics.CountMembers(selectedGroup.Id)}  infl {selectedGroup.Influence}";
        var units = selectedFaction is null ? [] : _host.Military.Units.ForFaction(selectedFaction.Id);
        var selectedUnit = units.Count > 0 ? units[_selectedUnitIndex % units.Count] : null;
        var militaryLine = selectedFaction is null
            ? "no military"
            : $"military units {units.Count}";
        var unitLine = selectedUnit is null
            ? "no military unit"
            : $"{selectedUnit.Id} {selectedUnit.Name}  {selectedUnit.Lifecycle}  members {_host.Military.CountMembers(selectedUnit.Id)}  {selectedUnit.Faction}";

        _label.Text =
            "CULTURES — PHASE 12  military\n" +
            $"{paused}   seed {_host.WorldSeed}   people {_host.Population.Alive.Count()}/{_host.Population.Count}   " +
            $"buildings {_host.Buildings.Count}   settlements {_host.Settlements.Count}   tick {date.Tick}\n" +
            $"cursor {cursor}   {chunk}   {terrain.Biome} {water} elev {terrain.Elevation:0.00}\n" +
            $"{characterLine}\n" +
            $"{familyLine}\n" +
            $"{skillLine}\n" +
            $"{buildingLine}\n" +
            $"{settlementLine}\n" +
            $"{lodLine}\n" +
            $"{explorationLine}\n" +
            $"{factionLine}\n" +
            $"{relationLine}\n" +
            $"{politicsLine}\n" +
            $"{groupLine}\n" +
            $"{militaryLine}\n" +
            $"{unitLine}\n" +
            $"last: {_lastCommand}\n" +
            "Arrows cursor   Tab person   C follow   B/V building   M/U settlement   E detect   L lod   O refresh   9 agg  0 full\n" +
            "P faction   J join   H diplomacy   I group   Y affiliate   W stability   1/2 influence   X unit   Z enlist   3 disband   Q overlay   R rumor   S scout   D map   F confirm   A analyze   Space";

        FitDebugHud();
        _map.QueueRedraw();
    }

    private void FitDebugHud()
    {
        var font = _label.GetThemeFont("font") ?? ThemeDB.FallbackFont;
        var fontSize = _label.GetThemeFontSize("font_size");
        if (fontSize <= 0)
            fontSize = DebugFontSize;

        var width = Mathf.Max(1f, Size.X - 24f);
        var textSize = font.GetMultilineStringSize(
            _label.Text,
            HorizontalAlignment.Left,
            width,
            fontSize);
        _hud.OffsetBottom = Mathf.Ceil(textSize.Y) + 16f;
    }
}
