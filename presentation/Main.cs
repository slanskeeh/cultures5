using Cultures.Application;
using Godot;

namespace Cultures.Presentation;

/// <summary>
/// Application shell only: boots the headless host, shows debug state,
/// and converts real time into explicit tick requests. Domain state lives
/// in <see cref="SimulationHost"/>, not in this Node.
/// </summary>
public partial class Main : Control
{
    public const double SecondsPerTick = 0.1;

    private SimulationHost _host = null!;
    private Label _label = null!;
    private double _accumulator;

    public override void _Ready()
    {
        _host = new SimulationHost(worldSeed: 1);
        _label = GetNode<Label>("DebugLabel");
        RefreshLabel();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
            return;

        if (key.Keycode == Key.Space)
        {
            _host.Clock.SetPaused(!_host.Clock.IsPaused);
            RefreshLabel();
        }
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

        RefreshLabel();
    }

    private void RefreshLabel()
    {
        var date = _host.Clock.Date;
        var paused = _host.Clock.IsPaused ? "PAUSED" : "RUNNING";
        _label.Text =
            "CULTURES — PHASE 0\n" +
            $"{paused}\n" +
            $"Seed {_host.WorldSeed}\n" +
            $"Tick {date.Tick}\n" +
            $"Year {date.Year}  Season {date.SeasonIndex}  Day {date.DayOfSeason}\n" +
            $"Hour {date.HourOfDay:00}:{date.MinuteOfHour:00}\n" +
            "Space — pause / resume";
    }
}
