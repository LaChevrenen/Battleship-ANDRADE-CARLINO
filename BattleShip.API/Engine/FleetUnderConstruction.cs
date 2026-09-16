using BattleShip.Models;

namespace BattleShip.API.Engine;

// Seul type modifiable de la préparation. Board, lui, reste immuable : chaque placement en produit une nouvelle.
// Game lâche cet objet au démarrage, ce qui rend tout placement ultérieur impossible faute d'objet à modifier.
public sealed class FleetUnderConstruction
{
    private readonly int[] allLengths;
    private readonly List<Ship> ships = [];
    private readonly List<int> remaining;

    public FleetUnderConstruction(int width, int height, IReadOnlyList<int> shipLengths)
    {
        Width = width;
        Height = height;
        allLengths = [.. shipLengths];
        remaining = [.. shipLengths];
    }

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyList<Ship> Ships => ships;
    public IReadOnlyList<int> RemainingLengths => remaining;
    public bool IsComplete => remaining.Count == 0;

    // Flotte déjà posée : sert au re-tirage aléatoire et aux parties construites pour les tests.
    public static FleetUnderConstruction Placed(int width, int height, IEnumerable<Ship> placed)
    {
        var cells = placed.ToList();
        var fleet = new FleetUnderConstruction(width, height, [.. cells.Select(ship => ship.Cells.Count)]);
        fleet.ships.AddRange(cells);
        fleet.remaining.Clear();
        return fleet;
    }

    public PlacementRejection? TryPlace(Coordinate origin, int length, Orientation orientation)
    {
        if (!remaining.Contains(length))
            return PlacementRejection.LengthNotAvailable;

        var cells = PlacementRules.Cells(origin, length, orientation);
        var rejection = PlacementRules.Check(cells, Width, Height, ships);
        if (rejection is not null)
            return rejection;

        ships.Add(new Ship(cells));
        remaining.Remove(length);
        return null;
    }

    public bool TryRemoveLast()
    {
        if (ships.Count == 0)
            return false;

        remaining.Add(ships[^1].Cells.Count);
        ships.RemoveAt(ships.Count - 1);
        return true;
    }

    public bool TryPlaceAtRandom(Random random)
    {
        var result = RandomFleetPlacer.Place(Width, Height, allLengths, random);
        if (result.Ships is null)
            return false;

        ships.Clear();
        ships.AddRange(result.Ships);
        remaining.Clear();
        return true;
    }

    public Board ToBoard() => new(Width, Height, ships);
}
