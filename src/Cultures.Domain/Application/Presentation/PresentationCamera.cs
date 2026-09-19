using Cultures.Buildings;
using Cultures.Core.Ids;
using Cultures.Population;
using Cultures.Settlement;
using Cultures.World;

namespace Cultures.Application.Presentation;

/// <summary>
/// Presentation-only camera. Not the simulation debug cursor.
/// </summary>
public sealed class PresentationCamera
{
    public PresentationCamera(LogicalGridCoordinate focus, int zoom = 22)
    {
        Focus = focus;
        Zoom = Math.Clamp(zoom, 8, 48);
    }

    public LogicalGridCoordinate Focus { get; set; }
    public int Zoom { get; set; }

    public void Follow(LogicalGridCoordinate cell) => Focus = cell;
}

/// <summary>
/// Accessibility and HUD preferences. Not simulation state.
/// </summary>
public sealed class PresentationSettings
{
    public bool HighContrast { get; set; }
    public bool ShowHelp { get; set; } = true;
    public int HudFontSize { get; set; } = 11;

    public void CycleHudFont()
    {
        HudFontSize = HudFontSize >= 16 ? 11 : HudFontSize + 2;
    }
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
