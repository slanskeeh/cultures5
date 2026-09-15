using Cultures.Core.Ids;
using Cultures.World;

namespace Cultures.Buildings;

public readonly record struct WorkplaceId(BuildingId Building, int Slot)
{
    public static WorkplaceId None { get; } = new(BuildingId.None, -1);
    public bool IsAssigned => Building.IsAssigned && Slot >= 0;
}

public sealed class WorkplaceSlot
{
    public WorkplaceSlot(int slot, LogicalGridCoordinate accessCell)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        AccessCell = accessCell;
    }

    public int Slot { get; }
    public LogicalGridCoordinate AccessCell { get; }
    public CharacterId Worker { get; set; } = CharacterId.None;

    public bool IsFree => !Worker.IsAssigned;
}
