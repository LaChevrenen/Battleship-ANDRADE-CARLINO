using BattleShip.Models;

namespace BattleShip.App.Services;

public static class ShipRotation
{
    public sealed record RotationDecision(bool RotatePortSelection, Coordinate? RotatePlacedShip);

    public static RotationDecision Resolve(int? selectedLength, Coordinate? hoveredCell, IReadOnlyCollection<Coordinate> ownShipCells)
    {
        if (selectedLength is not null)
            return new(true, null);

        if (hoveredCell is { } cell && ownShipCells.Contains(cell))
            return new(false, cell);

        return new(false, null);
    }
}
