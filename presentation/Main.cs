using Cultures.Application;
using Cultures.Population;
using Cultures.World;
using Cultures.World.Commands;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Application shell: clock, world cursor, and character debug inspect.
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

    private void Refresh()
    {
        var date = _host.Clock.Date;
        var cursor = _host.Cursor.Position;
        _host.World.Chunks.TryResolve(cursor.ToWorld(), out var chunk);
        _host.World.Grid.TryGetCell(cursor.ToWorld(), out var terrain);
        var paused = _host.Clock.IsPaused ? "PAUSED" : "RUNNING";
        var water = terrain.IsWater ? "water" : "land";
        var selected = _host.Population.Count > 0 ? _host.Population[_selectedIndex] : null;
        _map.SelectedId = selected?.Id;

        var characterLine = selected is null
            ? "no population"
            : $"{selected.Id} {selected.LifeStage} age {selected.AgeYears:0.0}  {selected.Position}  " +
              $"hunger {selected.Needs.Hunger:0.00}  fatigue {selected.Needs.Fatigue:0.00}  " +
              $"hp {selected.Health.Current:0.00}  food {selected.Inventory.Food}  {selected.Activity.Kind}";

        _label.Text =
            "CULTURES — PHASE 3  living characters\n" +
            $"{paused}   seed {_host.WorldSeed}   people {_host.Population.Alive.Count()}/{_host.Population.Count}   tick {date.Tick}\n" +
            $"cursor {cursor}   {chunk}   {terrain.Biome} {water} elev {terrain.Elevation:0.00}\n" +
            $"{characterLine}\n" +
            $"last: {_lastCommand}\n" +
            "Arrows cursor   Tab select   C follow   G mark   Space pause\n" +
            "white ring = selected   orange eat   blue sleep   brown work   white move";

        _map.QueueRedraw();
    }
}
