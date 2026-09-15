using BattleShip.Models;

namespace BattleShip.API.Engine;

public static class HuntTargetStrategy
{
    private static readonly (int Column, int Row)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    // Ne propose qu'une case de la grille jamais tirée : la boucle du tour de l'ordinateur en dépend pour terminer.
    public static Coordinate ChooseTarget(RevealedBoard view, Random random)
    {
        var sunkCells = view.SunkShips.SelectMany(cells => cells).ToHashSet();
        var unresolvedHits = view.Hits.Where(hit => !sunkCells.Contains(hit)).ToHashSet();

        var candidates = (unresolvedHits.Count > 0 ? TargetCells(unresolvedHits) : AllCells(view))
            .Where(cell => IsUntargeted(view, cell))
            .Distinct()
            // Ordre fixe avant le tirage : une même graine donne toujours la même case.
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Column)
            .ToList();

        return candidates[random.Next(candidates.Count)];
    }

    private static IEnumerable<Coordinate> TargetCells(HashSet<Coordinate> unresolvedHits)
    {
        foreach (var hit in unresolvedHits)
        {
            var isolated = !Directions.Any(direction => unresolvedHits.Contains(Step(hit, direction)));

            foreach (var direction in Directions)
            {
                // Touche isolée : ses 4 voisines. Touche alignée avec une autre : seulement le prolongement de la ligne.
                var continuesLine = unresolvedHits.Contains(Step(hit, (-direction.Column, -direction.Row)));
                if (isolated || continuesLine)
                    yield return Step(hit, direction);
            }
        }
    }

    private static IEnumerable<Coordinate> AllCells(RevealedBoard view) =>
        from row in Enumerable.Range(0, view.Height)
        from column in Enumerable.Range(0, view.Width)
        select new Coordinate(column, row);

    private static bool IsUntargeted(RevealedBoard view, Coordinate cell) =>
        cell.Column >= 0 && cell.Column < view.Width && cell.Row >= 0 && cell.Row < view.Height
        && !view.Misses.Contains(cell) && !view.Hits.Contains(cell);

    private static Coordinate Step(Coordinate cell, (int Column, int Row) direction) =>
        new(cell.Column + direction.Column, cell.Row + direction.Row);
}
