using System.Collections.Frozen;
using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Board(int width, int height, IEnumerable<Ship> ships)
{
    private readonly Ship[] fleet = [.. ships];

    // Réponses données au tireur, par case tirée : seule source de ce qui peut être révélé.
    private readonly Dictionary<Coordinate, ShotResult> answers = [];

    public IReadOnlyList<Ship> Ships => fleet;

    public bool AllShipsSunk => fleet.All(IsSunk);

    public ShotResult ReceiveShot(Coordinate target)
    {
        if (!IsInside(target))
            return ShotResult.Rejected(ShotRejection.OutOfBounds);

        if (answers.ContainsKey(target))
            return ShotResult.Rejected(ShotRejection.AlreadyTargeted);

        var result = Answer(target);
        // Enregistré en dernier, une fois tous les refus écartés : un tir refusé ne laisse aucune trace.
        answers.Add(target, result);
        return result;
    }

    // Construite depuis les réponses déjà données, jamais depuis la flotte : ce qui n'a pas été annoncé ne peut pas sortir.
    public RevealedBoard Reveal() => new(
        Width: width,
        Height: height,
        Misses: answers.Where(answer => answer.Value.Outcome == ShotOutcome.Miss).Select(answer => answer.Key).ToFrozenSet(),
        Hits: answers.Where(answer => answer.Value.Outcome is ShotOutcome.Hit or ShotOutcome.Sunk).Select(answer => answer.Key).ToFrozenSet(),
        SunkShips: [.. answers.Values.Select(answer => answer.SunkShip).OfType<Ship>().Select(ship => ship.Cells)]);

    private ShotResult Answer(Coordinate target)
    {
        var ship = fleet.FirstOrDefault(s => s.Occupies(target));
        if (ship is null)
            return ShotResult.Miss;

        var sunk = ship.Cells.All(cell => cell == target || answers.ContainsKey(cell));
        return sunk ? ShotResult.Sunk(ship) : ShotResult.Hit;
    }

    private bool IsInside(Coordinate cell) =>
        cell.Column >= 0 && cell.Column < width && cell.Row >= 0 && cell.Row < height;

    private bool IsSunk(Ship ship) => ship.Cells.All(answers.ContainsKey);
}
