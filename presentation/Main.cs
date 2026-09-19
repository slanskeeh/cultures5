using Cultures.Application;
using Cultures.Application.Persistence;
using Cultures.Application.Presentation;
using Cultures.Buildings;
using Cultures.Civilization;
using Cultures.Economy;
using Cultures.Core.Commands;
using Cultures.Core.Ids;
using Cultures.Exploration;
using Cultures.History;
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
    public const double SecondsPerTick = PresentationSettings.BaselineSecondsPerTick;
    public const int DebugFontSize = 11;

    private SimulationHost _host = null!;
    private ColorRect _hud = null!;
    private Label _label = null!;
    private WorldDebugMap _map = null!;
    private double _accumulator;
    private string _lastCommand = "ready";
    private int _selectedIndex = -1;
    private int _selectedBuildingIndex;
    private int _selectedSettlementIndex;
    private int _selectedFactionIndex;
    private int _selectedGroupIndex;
    private int _selectedUnitIndex;
    private int _startingJobIndex;
    private PresentationCamera _camera = null!;
    private readonly PresentationSelection _selection = new();
    private readonly PresentationSettings _settings = new();
    private FileSaveStore _saves = null!;
    private ColorRect _inspector = null!;
    private Label _inspectorLabel = null!;
    private WorldDebugMap _previewMap = null!;
    private bool _panning;

    public override void _Ready()
    {
        _host = new SimulationHost(
            worldSeed: 1,
            world: WorldConfiguration.Playtest,
            balance: new SimulationBalance { AutosaveIntervalTicks = 240, HuntFoodYield = 2 });
        _saves = new FileSaveStore(OS.GetUserDataDir());
        _hud = GetNode<ColorRect>("Hud");
        _label = GetNode<Label>("Hud/DebugLabel");
        _label.AddThemeFontSizeOverride("font_size", DebugFontSize);
        _label.VerticalAlignment = VerticalAlignment.Top;
        _map = GetNode<WorldDebugMap>("WorldDebugMap");
        _map.Host = _host;
        _map.Picked += OnMapPicked;
        _camera = PresentationCamera.LookingAt(_host.Cursor.Position, RenderProjection.Playtest, WorldDebugMap.CellSize);
        BuildInspector();
        Refresh();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Middle)
        {
            _panning = mouse.Pressed;
            if (mouse.Pressed)
                GetViewport().SetInputAsHandled();
            return;
        }

        if (!_panning || @event is not InputEventMouseMotion motion)
            return;

        _camera.Pan(-motion.Relative.X, -motion.Relative.Y);
        ConfineCamera();
        GetViewport().SetInputAsHandled();
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
            case Key.Key4:
                DebugMatchProfession();
                break;
            case Key.Key5:
                DebugFormHousehold();
                break;
            case Key.Key6:
                DebugSetHome();
                break;
            case Key.Key7:
                DebugHunt();
                break;
            case Key.Key8:
                DebugFormPact();
                break;
            case Key.Minus:
                _settings.Slower();
                _lastCommand = $"speed {_settings.PlaySpeed} ({_settings.PlaySpeedRate:P0})";
                break;
            case Key.Equal:
                _settings.Faster();
                _lastCommand = $"speed {_settings.PlaySpeed} ({_settings.PlaySpeedRate:P0})";
                break;
            case Key.F1:
                _settings.ShowHelp = !_settings.ShowHelp;
                if (_settings.ShowHelp)
                    _host.OnboardingComplete = true;
                _lastCommand = _settings.ShowHelp ? "help on" : "help off";
                break;
            case Key.F2:
                CycleStartingJob();
                break;
            case Key.F3:
                AssignStartingJob();
                break;
            case Key.F4:
                DirectSelectedLabor();
                break;
            case Key.F5:
                DebugSave();
                break;
            case Key.F6:
                StopSelectedLabor();
                break;
            case Key.F7:
                PlaceConstructingShelter();
                break;
            case Key.F9:
                DebugLoad();
                break;
            case Key.F11:
                _settings.HighContrast = !_settings.HighContrast;
                _map.HighContrast = _settings.HighContrast;
                _lastCommand = _settings.HighContrast ? "high contrast on" : "high contrast off";
                break;
            case Key.F12:
                _settings.CycleHudFont();
                _label.AddThemeFontSizeOverride("font_size", _settings.HudFontSize);
                _lastCommand = $"hud {_settings.HudFontSize}px";
                break;
            case Key.Comma:
                _camera.Zoom = Math.Max(8, _camera.Zoom - 2);
                _lastCommand = $"camera zoom {_camera.Zoom}";
                break;
            case Key.Period:
                _camera.Zoom = Math.Min(48, _camera.Zoom + 2);
                _lastCommand = $"camera zoom {_camera.Zoom}";
                break;
            default:
                return;
        }

        Refresh();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        ApplyEdgePan(delta);
        if (!_host.Clock.IsPaused)
        {
            _accumulator += delta;
            var stepSeconds = _settings.SecondsPerTick;
            while (_accumulator >= stepSeconds)
            {
                _accumulator -= stepSeconds;
                _host.Step(1);
            }
        }

        Refresh();
    }

    private void ApplyEdgePan(double delta)
    {
        if (_panning)
            return;

        var mouse = GetViewport().GetMousePosition();
        var rect = GetViewport().GetVisibleRect();
        if (!rect.HasPoint(mouse))
            return;

        var dx = 0f;
        var dy = 0f;
        var edge = PresentationCamera.EdgeMarginPixels;
        if (mouse.X <= rect.Position.X + edge)
            dx -= 1f;
        if (mouse.X >= rect.End.X - edge)
            dx += 1f;
        if (mouse.Y <= rect.Position.Y + edge)
            dy -= 1f;
        if (mouse.Y >= rect.End.Y - edge)
            dy += 1f;
        if (dx == 0f && dy == 0f)
            return;

        var speed = PresentationCamera.EdgeSpeedPerSecond * (float)delta;
        _camera.Pan(dx * speed, dy * speed);
        ConfineCamera();
    }

    private void ConfineCamera() =>
        _camera.Confine(_host.World.Configuration, RenderProjection.Playtest);

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
        var previous = TrySelected();
        _selectedIndex = _selectedIndex < 0
            ? 0
            : (_selectedIndex + 1) % _host.Population.Count;
        if (previous is { IsPersistentIndividual: true })
            _host.Commands.Execute(new ProtectCharacterCommand(previous.Id, false));
        ProtectSelected();
        var person = PersonOrFirst();
        _selection.Character = person.Id;
        _lastCommand = $"selected {person.Id}";
    }

    private void SnapToSelected()
    {
        if (TrySelected() is not { } character)
            return;
        var dx = _host.World.Topology.SignedHorizontalDelta(_host.Cursor.Position.X, character.Position.X);
        var dy = character.Position.Y - _host.Cursor.Position.Y;
        var result = _host.Commands.Execute(new MoveDebugCursorCommand(dx, dy));
        _camera.LookAt(character.Position, RenderProjection.Playtest);
        ConfineCamera();
        _lastCommand = result.Success ? $"camera to {character.Id}" : result.Error ?? "snap failed";
    }

    private void CycleBuilding()
    {
        if (_host.Buildings.Count == 0)
            return;
        _selectedBuildingIndex = (_selectedBuildingIndex + 1) % _host.Buildings.Count;
        _selection.Building = _host.Buildings[_selectedBuildingIndex].Id;
        _lastCommand = $"selected {_host.Buildings[_selectedBuildingIndex].Id}";
    }

    private void DebugCreateChild()
    {
        if (_host.Population.Count < 1)
            return;
        var parentA = PersonOrFirst();
        var parentB = _host.Population[(_selectedIndex + 1) % _host.Population.Count];
        var result = _host.Commands.Execute(new CreateChildCommand(parentA.Id, parentB.Id));
        _lastCommand = result.Success ? $"child of {parentA.Id}+{parentB.Id}" : result.Error ?? "birth failed";
    }

    private void DebugTeach()
    {
        if (_host.Population.Count < 1)
            return;
        var teacher = PersonOrFirst();
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
        var character = PersonOrFirst();
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
        _selection.Settlement = _host.Settlements[_selectedSettlementIndex].Id;
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
        _camera.LookAt(settlement.Core, RenderProjection.Playtest);
        ConfineCamera();
        _lastCommand = result.Success ? $"camera to {settlement.Id} core" : result.Error ?? "snap failed";
    }

    private void ProtectSelected()
    {
        if (TrySelected() is not { } character)
            return;
        _host.Commands.Execute(new ProtectCharacterCommand(character.Id, true));
    }

    private CharacterState? TrySelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _host.Population.Count)
            return null;
        return _host.Population[_selectedIndex];
    }

    private CharacterState PersonOrFirst() => TrySelected() ?? _host.Population[0];

    private bool SelectPerson(CharacterId id)
    {
        for (var i = 0; i < _host.Population.Count; i++)
        {
            if (_host.Population[i].Id != id)
                continue;
            _selectedIndex = i;
            _selection.Character = id;
            ProtectSelected();
            return true;
        }

        return false;
    }

    private void OnMapPicked(Vector2 local)
    {
        if (!_map.TryPick(local, out var cell, out var person))
        {
            _lastCommand = "click missed the map";
            Refresh();
            return;
        }

        if (person is { } id)
        {
            SelectPerson(id);
            _lastCommand = $"select {id}";
            Refresh();
            return;
        }

        if (TrySelected() is { } selected)
        {
            var result = _host.Commands.Execute(new OrderMoveCommand(selected.Id, cell));
            _lastCommand = result.Success ? $"{selected.Id} → {cell}" : result.Error ?? "move failed";
            Refresh();
            return;
        }

        _host.Commands.Execute(new SetDebugCursorCommand(cell));
        _lastCommand = $"cursor {cell}";
        Refresh();
    }

    private void BuildInspector()
    {
        _inspector = new ColorRect
        {
            Name = "Inspector",
            Color = new Color(0.07f, 0.08f, 0.055f, 0.94f),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false
        };
        _inspector.SetAnchorsPreset(LayoutPreset.BottomRight);
        _inspector.OffsetLeft = -300;
        _inspector.OffsetTop = -372;
        _inspector.OffsetRight = -12;
        _inspector.OffsetBottom = -12;
        AddChild(_inspector);

        var previewFrame = new ColorRect
        {
            Color = new Color(0.04f, 0.05f, 0.035f, 1f),
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = true
        };
        previewFrame.SetAnchorsPreset(LayoutPreset.TopWide);
        previewFrame.OffsetLeft = 8;
        previewFrame.OffsetTop = 8;
        previewFrame.OffsetRight = -8;
        previewFrame.OffsetBottom = 176;
        _inspector.AddChild(previewFrame);

        _previewMap = new WorldDebugMap
        {
            Host = _host,
            Interactive = false,
            ViewRadiusX = 6,
            ViewRadiusY = 4,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _previewMap.SetAnchorsPreset(LayoutPreset.FullRect);
        previewFrame.AddChild(_previewMap);

        _inspectorLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _inspectorLabel.AddThemeFontSizeOverride("font_size", DebugFontSize);
        _inspectorLabel.AddThemeColorOverride("font_color", new Color(0.82f, 0.74f, 0.52f));
        _inspectorLabel.SetAnchorsPreset(LayoutPreset.BottomWide);
        _inspectorLabel.OffsetLeft = 10;
        _inspectorLabel.OffsetTop = -188;
        _inspectorLabel.OffsetRight = -10;
        _inspectorLabel.OffsetBottom = -8;
        _inspector.AddChild(_inspectorLabel);
    }

    private void RefreshInspector(CharacterState? selected)
    {
        _inspector.Visible = selected is not null;
        if (selected is null)
        {
            _previewMap.FocusOverride = null;
            _previewMap.SelectedId = null;
            return;
        }

        _previewMap.Host = _host;
        _previewMap.FocusOverride = selected.Position;
        _previewMap.SelectedId = selected.Id;
        _previewMap.HighContrast = _settings.HighContrast;
        _inspectorLabel.Text =
            $"{selected.Name}  {selected.Id}\n" +
            $"{selected.LifeStage}  age {selected.AgeYears:0.0}\n" +
            $"job {selected.Profession}  {selected.Activity.Kind}\n" +
            $"hunger {selected.Needs.Hunger:0.00}  fatigue {selected.Needs.Fatigue:0.00}\n" +
            $"carry {FormatCargo(selected.Inventory)}\n" +
            $"click ground to walk   F4 work   F6 stop";
        _previewMap.QueueRedraw();
    }

    private ProfessionDefinition CurrentStartingJob()
    {
        var jobs = _host.Professions.Starting;
        return jobs[_startingJobIndex % jobs.Count];
    }

    private void CycleStartingJob()
    {
        if (_host.Professions.Starting.Count == 0)
            return;
        _startingJobIndex = (_startingJobIndex + 1) % _host.Professions.Starting.Count;
        _lastCommand = $"job {CurrentStartingJob().Id}";
    }

    private void AssignStartingJob()
    {
        if (TrySelected() is not { } person)
        {
            _lastCommand = "select a person first";
            return;
        }
        var job = CurrentStartingJob();
        var result = _host.Commands.Execute(new AssignProfessionCommand(person.Id, job.Id.Value));
        _lastCommand = result.Success ? $"{person.Id} → {job.Id}" : result.Error ?? "assign failed";
        ProtectSelected();
    }

    private void DirectSelectedLabor()
    {
        if (TrySelected() is not { } person)
        {
            _lastCommand = "select a person first";
            return;
        }
        var result = _host.Commands.Execute(new DirectLaborCommand(person.Id));
        _lastCommand = result.Success ? $"{person.Id} works now" : result.Error ?? "order failed";
    }

    private void StopSelectedLabor()
    {
        if (TrySelected() is not { } person)
        {
            _lastCommand = "select a person first";
            return;
        }
        var result = _host.Commands.Execute(new StopLaborCommand(person.Id));
        _lastCommand = result.Success ? $"{person.Id} stopped" : result.Error ?? "stop failed";
    }

    private void PlaceConstructingShelter()
    {
        var origin = _host.Cursor.Position;
        var result = _host.Commands.Execute(
            new PlaceBuildingCommand(BuildingTypeId.Shelter, origin, CompleteImmediately: false));
        if (result.Success)
        {
            _lastCommand = $"hut constructing at {origin}";
            return;
        }

        foreach (var cell in SettlementSpiral(origin))
        {
            result = _host.Commands.Execute(
                new PlaceBuildingCommand(BuildingTypeId.Shelter, cell, CompleteImmediately: false));
            if (!result.Success)
                continue;
            _lastCommand = $"hut constructing at {cell}";
            _host.Commands.Execute(new SetDebugCursorCommand(cell));
            return;
        }

        _lastCommand = result.Error ?? "no room for a hut";
    }

    private IEnumerable<LogicalGridCoordinate> SettlementSpiral(LogicalGridCoordinate origin)
    {
        for (var radius = 1; radius <= 12; radius++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    var resolution = _host.World.Topology.Resolve(origin.X + dx, origin.Y + dy);
                    if (resolution.TryGetCell(out var cell))
                        yield return cell;
                }
            }
        }
    }

    private static string FormatCargo(Inventory inventory)
    {
        var parts = inventory.Enumerate().Select(stack => $"{stack.Type} x{stack.Quantity}").ToArray();
        return parts.Length == 0 ? "empty" : string.Join(" ", parts);
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
        var person = PersonOrFirst();
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
        var person = PersonOrFirst();
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
        var person = PersonOrFirst();
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

    private void DebugMatchProfession()
    {
        if (_host.Population.Count == 0)
            return;
        var person = PersonOrFirst();
        var result = _host.Commands.Execute(new MatchProfessionCommand(person.Id));
        _lastCommand = result.Success
            ? $"{person.Id} profession {person.Profession}"
            : result.Error ?? "profession failed";
    }

    private void DebugFormHousehold()
    {
        if (_host.Population.Count < 2)
            return;
        var a = PersonOrFirst();
        var b = _host.Population[(_selectedIndex + 1) % _host.Population.Count];
        var result = _host.Commands.Execute(new FormPartnershipCommand(a.Id, b.Id));
        if (!result.Success)
            result = _host.Commands.Execute(new FormHouseholdCommand(a.Id, b.Id));
        _lastCommand = result.Success
            ? $"{a.Id} household {a.Household}"
            : result.Error ?? "household failed";
    }

    private void DebugSetHome()
    {
        if (_host.Population.Count == 0)
            return;
        var person = PersonOrFirst();
        if (!person.Household.IsAssigned)
        {
            _lastCommand = "no household";
            return;
        }

        var shelter = _host.Buildings.All.FirstOrDefault(b => b.Definition.IsShelter && b.IsActive);
        if (shelter is null)
        {
            _lastCommand = "no shelter";
            return;
        }

        var result = _host.Commands.Execute(new SetHouseholdHomeCommand(person.Household, shelter.Id));
        _lastCommand = result.Success ? $"home {shelter.Id}" : result.Error ?? "home failed";
    }

    private void DebugHunt()
    {
        if (_host.Population.Count == 0)
            return;
        var person = PersonOrFirst();
        var result = _host.Commands.Execute(new HuntWildlifeCommand(person.Id));
        _lastCommand = result.Success
            ? $"{person.Id} hunted food {person.Inventory.GetQuantity(ResourceType.Food)}"
            : result.Error ?? "hunt failed";
    }

    private void DebugFormPact()
    {
        if (_host.Civilization.Factions.Count < 2)
            return;
        var a = _host.Civilization.Factions[_selectedFactionIndex % _host.Civilization.Factions.Count];
        var b = _host.Civilization.Factions[(_selectedFactionIndex + 1) % _host.Civilization.Factions.Count];
        var result = _host.Commands.Execute(new FormDiplomaticPactCommand(a.Id, b.Id, DiplomaticPactKind.Trade));
        _lastCommand = result.Success ? $"pact {a.Name} ↔ {b.Name}" : result.Error ?? "pact failed";
    }

    private void DebugSave()
    {
        _host.WriteSave(_saves, SaveSlots.Default);
        _lastCommand = $"saved {SaveSlots.Default}";
    }

    private void DebugLoad()
    {
        try
        {
            _host = SimulationHost.LoadSave(_saves, SaveSlots.Default);
            _map.Host = _host;
            _previewMap.Host = _host;
            _selectedIndex = -1;
            _camera = PresentationCamera.LookingAt(_host.Cursor.Position, RenderProjection.Playtest, _camera.Zoom);
            _lastCommand = $"loaded tick {_host.Clock.Tick}";
        }
        catch (Exception ex)
        {
            _lastCommand = $"load failed: {ex.Message}";
        }
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
        _camera.LookAt(building.Origin, RenderProjection.Playtest);
        ConfineCamera();
        _lastCommand = result.Success ? $"camera to {building.Id}" : result.Error ?? "snap failed";
    }

    private void Refresh()
    {
        var date = _host.Clock.Date;
        var cursor = _host.Cursor.Position;
        _host.World.Chunks.TryResolve(cursor.ToWorld(), out var chunk);
        _host.World.Grid.TryGetCell(cursor.ToWorld(), out var terrain);
        var paused = _host.Clock.IsPaused ? "PAUSED" : "RUNNING";
        var water = terrain.IsWater ? "water" : "land";
        var selected = TrySelected();
        var selectedBuilding = _host.Buildings.Count > 0 ? _host.Buildings[_selectedBuildingIndex] : null;
        var selectedSettlement = _host.Settlements.Count > 0
            ? _host.Settlements[_selectedSettlementIndex % _host.Settlements.Count]
            : null;
        var cursorBuilding = _host.Buildings.FindAt(cursor);
        var inspectBuilding = cursorBuilding ?? selectedBuilding;
        _map.SelectedId = selected?.Id;
        _map.SelectedBuildingId = inspectBuilding?.Id;
        _map.SelectedSettlementId = selectedSettlement?.Id;
        RefreshInspector(selected);

        var workplace = selected is { AssignedWorkplace.IsAssigned: true }
            ? selected.AssignedWorkplace.ToString()
            : "none";
        var cargo = selected is null ? "empty" : FormatCargo(selected.Inventory);
        var characterLine = selected is null
            ? "no population"
            : $"{selected.Id} {selected.LifeStage} age {selected.AgeYears:0.0}  {selected.Position}  " +
              $"hunger {selected.Needs.Hunger:0.00}  fatigue {selected.Needs.Fatigue:0.00}  " +
              $"carry {cargo}  {selected.Activity.Kind}  " +
              $"work {workplace}  {selected.Settlement}  {selected.Culture}  {selected.Faction}  {selected.PoliticalGroup}  {selected.MilitaryUnit}  job {selected.Profession}  home {selected.Household}  lod {selected.LodTier}";
        var familyLine = selected is null
            ? ""
            : $"parents {string.Join(",", selected.FamilyLinks.Parents.Select(id => id.Value.ToString()))}  " +
              $"care {string.Join(",", selected.FamilyLinks.Caregivers.Select(id => id.Value.ToString()))}  " +
              $"children {string.Join(",", selected.FamilyLinks.Children.Select(id => id.Value.ToString()))}  " +
              $"partner {selected.Activity.PartnerId}  skill {selected.Activity.Skill?.ToString() ?? "-"}";
        var skillLine = selected is null
            ? ""
            :               $"farm {selected.Skills.GetLevel(SkillType.Farming)}  " +
              $"wood {selected.Skills.GetLevel(SkillType.Woodworking)}  " +
              $"stone {selected.Skills.GetLevel(SkillType.Stoneworking)}  " +
              $"craft {selected.Skills.GetLevel(SkillType.Crafting)}  " +
              $"hunt {selected.Skills.GetLevel(SkillType.Hunting)}  " +
              $"fish {selected.Skills.GetLevel(SkillType.Fishing)}";

        var buildingLine = inspectBuilding is null
            ? "no building"
            : $"{inspectBuilding.Id} {inspectBuilding.TypeId} {inspectBuilding.Lifecycle} {inspectBuilding.Origin}  " +
              $"build {inspectBuilding.ConstructionProgress}/{inspectBuilding.Definition.ConstructionTicks}  " +
              $"food {inspectBuilding.Inventory.GetQuantity(ResourceType.Food)}  " +
              $"wood {inspectBuilding.Inventory.GetQuantity(ResourceType.Wood)}  " +
              $"stone {inspectBuilding.Inventory.GetQuantity(ResourceType.Stone)}  " +
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
        var historyLine = HistoryChronicle.RenderRecent(_host.History, 5);
        var pacts = _host.Diplomacy.Pacts.Count;
        var help = _settings.ShowHelp ? $"{PlayGuide.Intro}\n{PlayGuide.Controls}\n" : "";
        var jobPreview = _host.Professions.Starting.Count == 0
            ? "-"
            : CurrentStartingJob().Id.Value;
        var playableLine =
            $"KINLANDS  {paused}  speed {_settings.PlaySpeed} ({_settings.PlaySpeedRate:P0})  people {_host.Population.Alive.Count()}/{_host.Population.Count}  tick {date.Tick}\n" +
            $"selected {(selected is null ? "none" : $"{selected.Id}  job {selected.Profession}  {selected.Activity.Kind}  hunger {selected.Needs.Hunger:0.00}  carry {cargo}")}\n" +
            $"site {(inspectBuilding is null ? "none" : $"{inspectBuilding.TypeId} {inspectBuilding.Lifecycle} {inspectBuilding.ConstructionProgress}/{inspectBuilding.Definition.ConstructionTicks} wood {inspectBuilding.Inventory.GetQuantity(ResourceType.Wood)}")}\n" +
            $"ready {jobPreview}   click select   F2 job   F3 assign   F4 work   F6 stop   F7 hut   Space pause   F1 help";
        var fertility = terrain.Fertility;
        var wildlife = _host.World.Chunks.TryResolve(cursor.ToWorld(), out var address)
            && _host.Ecology.Wildlife.TryGet(address.Chunk, out var pop)
            ? $"deer {pop.Deer} sheep {pop.Sheep} boar {pop.Boar} birds {pop.Birds}"
            : "wildlife -";
        _map.CameraIsoX = _camera.IsoX;
        _map.CameraIsoY = _camera.IsoY;

        _label.Text =
            help +
            $"{playableLine}\n" +
            $"[sim] seed {_host.WorldSeed}  buildings {_host.Buildings.Count}  settlements {_host.Settlements.Count}  {_host.Diagnostics.Render()}\n" +
            $"[world] cursor {cursor}  cam {_camera.IsoX:0.0},{_camera.IsoY:0.0} z{_camera.Zoom}  {chunk}  {terrain.Biome} {water} " +
            $"elev {terrain.Elevation:0.00} fert {fertility:0.00} river {terrain.HasRiver}  {wildlife}\n" +
            $"[entity] {characterLine}\n" +
            $"{familyLine}\n" +
            $"{skillLine}\n" +
            $"{buildingLine}\n" +
            $"{settlementLine}\n" +
            $"[explore] {lodLine}\n" +
            $"{explorationLine}\n" +
            $"[faction] {factionLine}\n" +
            $"{relationLine}  pacts {pacts}\n" +
            $"{politicsLine}\n" +
            $"{groupLine}\n" +
            $"[military] {militaryLine}\n" +
            $"{unitLine}\n" +
            $"[history] {historyLine}\n" +
            $"last: {_lastCommand}\n" +
            "Arrows debug cursor   MMB pan   edge scroll   Tab person   C look   B/V building   M/U settlement   E detect   L lod   O refresh   9 agg  0 full\n" +
            "P faction   J join   H diplomacy   I group   Y affiliate   W stability   1/2 influence   X unit   Z enlist   3 disband\n" +
            "4 match trained job   5 household   6 home   7 hunt   8 pact   -/= speed 1-3   F5 save   F9 load   F11 contrast   F12 HUD\n" +
            ",/. camera zoom   Q overlay   R rumor   S scout   D map   F confirm   A analyze";

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
