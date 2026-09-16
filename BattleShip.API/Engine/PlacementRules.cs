using BattleShip.Models;

namespace BattleShip.API.Engine;

// Règles de placement d'un navire, partagées par le placement aléatoire et le placement manuel.
public static class PlacementRules
{
    public static Coordinate[] Cells(Coordinate origin, int length, Orientation orientation) =>
    [
        .. Enumerable.Range(0, length).Select(offset => orientation == Orientation.Horizontal
            ? origin with { Column = origin.Column + offset }
            : origin with { Row = origin.Row + offset })
    ];

    public static PlacementRejection? Check(IReadOnlyCollection<Coordinate> cells, int width, int height, IEnumerable<Ship> placed) =>
        Check(cells, width, height, placed.SelectMany(ship => ship.Cells).ToHashSet());

    public static PlacementRejection? Check(IReadOnlyCollection<Coordinate> cells, int width, int height, IReadOnlySet<Coordinate> occupied)
    {
        if (cells.Any(cell => cell.Column < 0 || cell.Column >= width || cell.Row < 0 || cell.Row >= height))
            return PlacementRejection.OutOfBounds;

        if (cells.Any(occupied.Contains))
            return PlacementRejection.Overlap;

        // Contact interdit par un côté ; le contact en diagonale reste autorisé.
        if (cells.Any(cell => SideNeighbours(cell).Any(occupied.Contains)))
            return PlacementRejection.AdjacentShip;

        return null;
    }

    public static Coordinate[] SideNeighbours(Coordinate cell) =>
    [
        cell with { Column = cell.Column - 1 },
        cell with { Column = cell.Column + 1 },
        cell with { Row = cell.Row - 1 },
        cell with { Row = cell.Row + 1 }
    ];
}
