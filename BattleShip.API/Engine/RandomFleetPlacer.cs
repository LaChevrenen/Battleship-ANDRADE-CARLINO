using BattleShip.Models;

namespace BattleShip.API.Engine;

public static class RandomFleetPlacer
{
    private static readonly (int Column, int Row)[] Directions = [(1, 0), (0, 1)];

    // Un essai est une flotte complète : si un navire n'a plus de position légale, on recommence tout.
    public static PlacementResult Place(int width, int height, IReadOnlyList<int> shipLengths, Random random)
    {
        var longestFirst = shipLengths.OrderDescending().ToArray();
        var attempts = 0;

        while (attempts < GameRules.MaxPlacementAttempts)
        {
            attempts++;
            var ships = TryPlaceFleet(width, height, longestFirst, random);
            if (ships is not null)
                return new PlacementResult(new Board(width, height, ships), attempts);
        }

        // Le compteur réel, pas la constante : sinon une boucle mal bornée annoncerait quand même 1000.
        return new PlacementResult(null, attempts);
    }

    private static List<Ship>? TryPlaceFleet(int width, int height, int[] lengths, Random random)
    {
        var ships = new List<Ship>();
        // Cases occupées et leurs voisines par un côté : le contact en diagonale reste autorisé.
        var blocked = new HashSet<Coordinate>();

        foreach (var length in lengths)
        {
            var candidates = LegalPositions(width, height, length, blocked).ToList();
            if (candidates.Count == 0)
                return null;

            var ship = new Ship(candidates[random.Next(candidates.Count)]);
            ships.Add(ship);
            foreach (var cell in ship.Cells)
            {
                blocked.Add(cell);
                blocked.UnionWith(SideNeighbours(cell));
            }
        }

        return ships;
    }

    private static Coordinate[] SideNeighbours(Coordinate cell) =>
    [
        cell with { Column = cell.Column - 1 },
        cell with { Column = cell.Column + 1 },
        cell with { Row = cell.Row - 1 },
        cell with { Row = cell.Row + 1 }
    ];

    private static IEnumerable<Coordinate[]> LegalPositions(int width, int height, int length, HashSet<Coordinate> blocked)
    {
        foreach (var direction in Directions)
        {
            for (var column = 0; column + direction.Column * (length - 1) < width; column++)
            {
                for (var row = 0; row + direction.Row * (length - 1) < height; row++)
                {
                    var cells = Enumerable.Range(0, length)
                        .Select(i => new Coordinate(column + direction.Column * i, row + direction.Row * i))
                        .ToArray();

                    if (!cells.Any(blocked.Contains))
                        yield return cells;
                }
            }
        }
    }
}
