using Cultures.Application;
using Cultures.World;
using Cultures.World.Commands;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Application shell: clock + logical-world debug cursor. Domain state is not stored on this Node.
/// </summary>
public partial class Main : Control
{
    public const double SecondsPerTick = 0.1;

    private SimulationHost _host = null!;
    private Label _label = null!;
    private WorldDebugMap _map = null!;
    private double _accumulator;
    private string _lastCommand = "ready";
    private readonly RenderProjection _projection = new(32, 16);

    public override void _Ready()
    {
        _host = new SimulationHost(worldSeed: 1, world: WorldConfiguration.DebugSample);
        _label = GetNode<Label>("Hud/DebugLabel");
        _map = GetNode<WorldDebugMap>("WorldDebugMap");
        _map.Host = _host;
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

    private void Refresh()
    {
        var date = _host.Clock.Date;
        var cursor = _host.Cursor.Position;
        _host.World.Chunks.TryResolve(cursor.ToWorld(), out var chunk);
        var iso = _projection.ToIsometric(cursor);
        var screen = _projection.ToScreen(iso, new ScreenCoordinate(640, 360));
        _host.World.Grid.TryGetOccupancy(cursor.ToWorld(), out var occupancy);
        var paused = _host.Clock.IsPaused ? "PAUSED" : "RUNNING";

        _label.Text =
            "CULTURES — PHASE 1  logical world\n" +
            $"{paused}   seed {_host.WorldSeed}   tick {date.Tick}\n" +
            $"cursor  {cursor}\n" +
            $"{chunk}\n" +
            $"iso {iso}   screen {screen} (debug mapping only)\n" +
            $"occupancy {occupancy}\n" +
            $"last: {_lastCommand}\n" +
            "Arrows move   G occupy/clear   Space pause\n" +
            "X wraps   Y stops at poles   gold = cursor   bright column = seam";

        _map.QueueRedraw();
    }
}
