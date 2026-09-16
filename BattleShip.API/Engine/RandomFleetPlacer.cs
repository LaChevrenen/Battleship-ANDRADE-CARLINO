using BattleShip.Models;

namespace BattleShip.API.Engine;

public static class RandomFleetPlacer
{
    private static readonly Orientation[] Orientations = [Orientation.Horizontal, Orientation.Vertical];

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
                return new PlacementResult(ships, attempts);
        }

        // Le compteur réel, pas la constante : sinon une boucle mal bornée annoncerait quand même 1000.
        return new PlacementResult(null, attempts);
    }

    private static List<Ship>? TryPlaceFleet(int width, int height, int[] lengths, Random random)
    {
        var ships = new List<Ship>();
        var occupied = new HashSet<Coordinate>();

        foreach (var length in lengths)
        {
            // Mêmes règles que le placement manuel : la validité est jugée par PlacementRules.
            var candidates = LegalPositions(width, height, length, occupied).ToList();
            if (candidates.Count == 0)
                return null;

            var cells = candidates[random.Next(candidates.Count)];
            ships.Add(new Ship(cells));
            occupied.UnionWith(cells);
        }

        return ships;
    }

    // Piste si le placement devenait trop lent : reconstituer ici un ensemble de cases bloquées
    // (cases occupées et leurs voisines) au lieu d'interroger PlacementRules pour chaque candidate.
    // Non fait : cela remettrait la règle de contact à deux endroits.
    private static IEnumerable<Coordinate[]> LegalPositions(int width, int height, int length, IReadOnlySet<Coordinate> occupied)
    {
        foreach (var orientation in Orientations)
        {
            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    var cells = PlacementRules.Cells(new Coordinate(column, row), length, orientation);
                    if (PlacementRules.Check(cells, width, height, occupied) is null)
                        yield return cells;
                }
            }
        }
    }
}
