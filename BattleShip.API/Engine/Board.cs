using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Board(int width, int height, IEnumerable<Ship> ships)
{
    private readonly Ship[] fleet = [.. ships];
    private readonly HashSet<Coordinate> shots = [];

    public IReadOnlyList<Ship> Ships => fleet;

    public bool AllShipsSunk => fleet.All(IsSunk);

    public ShotResult ReceiveShot(Coordinate target)
    {
        if (!IsInside(target))
            return ShotResult.Rejected(ShotRejection.OutOfBounds);

        // Add enregistre le tir : tout motif de refus doit être testé avant, sinon un tir refusé modifierait la grille.
        if (!shots.Add(target))
            return ShotResult.Rejected(ShotRejection.AlreadyTargeted);

        var ship = fleet.FirstOrDefault(s => s.Occupies(target));
        if (ship is null)
            return ShotResult.Miss;

        return IsSunk(ship) ? ShotResult.Sunk(ship) : ShotResult.Hit;
    }

    private bool IsInside(Coordinate cell) =>
        cell.Column >= 0 && cell.Column < width && cell.Row >= 0 && cell.Row < height;

    private bool IsSunk(Ship ship) => ship.Cells.All(shots.Contains);
}
