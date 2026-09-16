using BattleShip.Models;

namespace BattleShip.API.Engine;

public static class ProbabilityTargetStrategy
{
    private static readonly int[] ShipLengths = [5, 4, 3, 3, 2];

    public static Coordinate ChooseTarget(RevealedBoard view, Random random)
    {
        var alreadyTargeted = view.Misses.Concat(view.Hits).ToHashSet();
        var candidates = AllCells(view).Where(cell => !alreadyTargeted.Contains(cell)).ToList();
        var unresolvedHits = view.Hits.Except(view.SunkShips.SelectMany(ship => ship)).ToHashSet();

        var scores = candidates.ToDictionary(cell => cell, _ => 0);
        foreach (var length in ShipLengths)
        foreach (var cells in Placements(view, length))
        {
            if (cells.Any(view.Misses.Contains))
                continue;

            if (unresolvedHits.Count > 0 && !cells.Any(unresolvedHits.Contains))
                continue;

            foreach (var cell in cells.Where(cell => !alreadyTargeted.Contains(cell)))
                scores[cell]++;
        }

        var bestScore = scores.Values.Max();
        var best = scores.Where(pair => pair.Value == bestScore).Select(pair => pair.Key).ToList();
        return best[random.Next(best.Count)];
    }

    private static IEnumerable<Coordinate> AllCells(RevealedBoard view) =>
        from row in Enumerable.Range(0, view.Height)
        from column in Enumerable.Range(0, view.Width)
        select new Coordinate(column, row);

    private static IEnumerable<IReadOnlyList<Coordinate>> Placements(RevealedBoard view, int length)
    {
        foreach (var row in Enumerable.Range(0, view.Height))
        foreach (var column in Enumerable.Range(0, view.Width))
        {
            var horizontal = Enumerable.Range(0, length)
                .Select(offset => new Coordinate(column + offset, row)).ToArray();
            if (horizontal.All(cell => cell.Column < view.Width))
                yield return horizontal;

            var vertical = Enumerable.Range(0, length)
                .Select(offset => new Coordinate(column, row + offset)).ToArray();
            if (vertical.All(cell => cell.Row < view.Height))
                yield return vertical;
        }
    }
}