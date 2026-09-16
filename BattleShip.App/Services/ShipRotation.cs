using BattleShip.Models;

namespace BattleShip.App.Services;

public static class ShipRotation
{
    public sealed record RotationDecision(bool RotatePortSelection, Coordinate? RotatePlacedShip);

    public static RotationDecision Resolve(int? selectedLength, IReadOnlyList<Coordinate>? selectedPlacedShip)
    {
        if (selectedPlacedShip is { Count: > 0 })
        {
            var averageColumn = selectedPlacedShip.Average(cell => cell.Column);
            var averageRow = selectedPlacedShip.Average(cell => cell.Row);
            var center = selectedPlacedShip
                .OrderBy(cell => Math.Abs(cell.Column - averageColumn) + Math.Abs(cell.Row - averageRow))
                .First();
            return new(false, center);
        }

        if (selectedLength is not null)
            return new(true, null);

        return new(false, null);
    }
}
