using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.Application.Presentation;

/// <summary>
/// Presentation-only camera in continuous isometric space. Not the simulation cursor and not a hex.
/// </summary>
public sealed class PresentationCamera
{
    public const float EdgeMarginPixels = 28f;
    public const float EdgeSpeedPerSecond = 420f;

    public PresentationCamera(float isoX, float isoY, int zoom = 22)
    {
        IsoX = isoX;
        IsoY = isoY;
        Zoom = Math.Clamp(zoom, 8, 48);
    }

    public float IsoX { get; set; }
    public float IsoY { get; set; }
    public int Zoom { get; set; }

    public static PresentationCamera LookingAt(LogicalGridCoordinate cell, RenderProjection projection, int zoom = 22)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var iso = projection.ToIso(cell);
        return new PresentationCamera(iso.X, iso.Y, zoom);
    }

    public void LookAt(LogicalGridCoordinate cell, RenderProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var iso = projection.ToIso(cell);
        IsoX = iso.X;
        IsoY = iso.Y;
    }

    public void Pan(float isoDeltaX, float isoDeltaY)
    {
        IsoX += isoDeltaX;
        IsoY += isoDeltaY;
    }

    public void Confine(WorldConfiguration world, RenderProjection projection)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(projection);
        var width = projection.HexWidth * world.Width;
        var height = projection.HexDepth * projection.VerticalScale * Math.Max(0, world.Height - 1);
        IsoY = Math.Clamp(IsoY, 0f, height);
        IsoX = Repeat(IsoX, width);
    }

    public LogicalGridCoordinate ApproximateCell(RenderProjection projection, WorldTopology topology)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(topology);
        var raw = projection.ApproximateCell(IsoX, IsoY);
        var resolution = topology.Resolve(raw.X, raw.Y);
        if (resolution.TryGetCell(out var cell))
            return cell;
        return new LogicalGridCoordinate(
            topology.WrapX(raw.X),
            Math.Clamp(raw.Y, 0, topology.Configuration.Height - 1));
    }

    private static float Repeat(float value, float modulus)
    {
        if (modulus <= 0f)
            return 0f;
        var wrapped = value % modulus;
        return wrapped < 0f ? wrapped + modulus : wrapped;
    }
}

/// <summary>
/// Accessibility and HUD preferences. Not simulation state.
/// </summary>
public sealed class PresentationSettings
{
    public const double BaselineSecondsPerTick = 0.1;

    public static readonly float[] PlaySpeedRates = [0.25f, 0.40f, 0.60f];

    public bool HighContrast { get; set; }
    public bool ShowHelp { get; set; } = true;
    public int HudFontSize { get; set; } = 11;
    public int PlaySpeed { get; private set; } = 1;

    public float PlaySpeedRate => PlaySpeedRates[PlaySpeed - 1];

    public double SecondsPerTick => BaselineSecondsPerTick / PlaySpeedRate;

    public void CycleHudFont()
    {
        HudFontSize = HudFontSize >= 16 ? 11 : HudFontSize + 2;
    }

    public void SetPlaySpeed(int speed) => PlaySpeed = Math.Clamp(speed, 1, PlaySpeedRates.Length);

    public void Faster() => SetPlaySpeed(PlaySpeed + 1);

    public void Slower() => SetPlaySpeed(PlaySpeed - 1);
}

/// <summary>
/// Selection by stable domain IDs. Does not mutate simulation.
/// </summary>
public sealed class PresentationSelection
{
    public CharacterId Character { get; set; }
    public BuildingId Building { get; set; }
    public SettlementId Settlement { get; set; }
}

/// <summary>
/// Recreatable view identity set derived from domain IDs inside a camera window.
/// </summary>
public static class PresentationIdentityMap
{
    public static PresentationIdentitySnapshot Capture(
        IReadOnlyList<CharacterState> people,
        IReadOnlyList<BuildingState> buildings,
        IReadOnlyList<SettlementState> settlements,
        LogicalGridCoordinate focus,
        WorldTopology topology,
        int radiusX,
        int radiusY)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(settlements);
        ArgumentNullException.ThrowIfNull(topology);

        var characters = new List<ulong>();
        foreach (var person in people)
        {
            if (InView(topology, focus, person.Position, radiusX, radiusY))
                characters.Add(person.Id.Value);
        }

        var buildingIds = new List<ulong>();
        foreach (var building in buildings)
        {
            if (InView(topology, focus, building.Origin, radiusX, radiusY))
                buildingIds.Add(building.Id.Value);
        }

        var settlementIds = new List<ulong>();
        foreach (var settlement in settlements)
        {
            if (InView(topology, focus, settlement.Core, radiusX, radiusY))
                settlementIds.Add(settlement.Id.Value);
        }

        return new PresentationIdentitySnapshot(characters, buildingIds, settlementIds);
    }

    public static bool InView(
        WorldTopology topology,
        LogicalGridCoordinate focus,
        LogicalGridCoordinate cell,
        int radiusX,
        int radiusY)
    {
        var dx = Math.Abs(topology.SignedHorizontalDelta(focus.X, cell.X));
        var dy = Math.Abs(cell.Y - focus.Y);
        return dx <= radiusX && dy <= radiusY;
    }
}

public readonly record struct PresentationIdentitySnapshot(
    IReadOnlyList<ulong> Characters,
    IReadOnlyList<ulong> Buildings,
    IReadOnlyList<ulong> Settlements);
