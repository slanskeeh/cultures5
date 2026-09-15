using Cultures.Application;
using Cultures.Buildings;
using Cultures.Economy;
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

    private SimulationHost _host = null!;
    private Label _label = null!;
    private WorldDebugMap _map = null!;
    private double _accumulator;
    private string _lastCommand = "ready";
    private int _selectedIndex;
    private int _selectedBuildingIndex;
    private int _selectedSettlementIndex;

    public override void _Ready()
    {
        _host = new SimulationHost(worldSeed: 1, world: WorldConfiguration.DebugSample);
        _label = GetNode<Label>("Hud/DebugLabel");
        _map = GetNode<WorldDebugMap>("WorldDebugMap");
        _map.Host = _host;
        SnapToSelected();
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
        _selectedIndex = (_selectedIndex + 1) % _host.Population.Count;
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
              $"work {workplace}  {selected.Settlement}";
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

        _label.Text =
            "CULTURES — PHASE 6  emergent settlements\n" +
            $"{paused}   seed {_host.WorldSeed}   people {_host.Population.Alive.Count()}/{_host.Population.Count}   " +
            $"buildings {_host.Buildings.Count}   settlements {_host.Settlements.Count}   tick {date.Tick}\n" +
            $"cursor {cursor}   {chunk}   {terrain.Biome} {water} elev {terrain.Elevation:0.00}\n" +
            $"{characterLine}\n" +
            $"{familyLine}\n" +
            $"{skillLine}\n" +
            $"{buildingLine}\n" +
            $"{settlementLine}\n" +
            $"last: {_lastCommand}\n" +
            "Arrows cursor   Tab person   C follow   B/V building   M/U settlement   E detect   N child   T teach   K skill   G mark   Space pause\n" +
            "F farm  S storage  H house  W workshop";

        _map.QueueRedraw();
    }
}
