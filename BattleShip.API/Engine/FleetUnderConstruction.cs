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

    // Pivot autour de l'origine du navire : sa case en haut à gauche ne bouge pas.
    // La validité est jugée AVANT toute modification, et contre les autres navires seulement —
    // un navire qui se gênerait lui-même rendrait toute rotation impossible. Sur un refus, rien
    // n'a été retiré : il n'existe aucun instant où la flotte est incomplète.
    public PlacementRejection? TryRotateAt(Coordinate target)
    {
        var ship = ships.FirstOrDefault(placed => placed.Occupies(target));
        if (ship is null)
            return PlacementRejection.NoShipHere;

        var origin = ship.Cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).First();
        var horizontal = ship.Cells.All(cell => cell.Row == origin.Row);
        var turned = horizontal ? Orientation.Vertical : Orientation.Horizontal;
        var cells = PlacementRules.Cells(origin, ship.Cells.Count, turned);

        var rejection = PlacementRules.Check(cells, Width, Height, ships.Where(placed => placed != ship));
        if (rejection is not null)
            return rejection;

        ships.Remove(ship);
        ships.Add(new Ship(cells));
        return null;
    }

    public bool TryRemoveAt(Coordinate cell)
    {
        var ship = ships.FirstOrDefault(placed => placed.Occupies(cell));
        if (ship is null)
            return false;

        ships.Remove(ship);
        remaining.Add(ship.Cells.Count);
        return true;
    }

    // Origines où ce navire tient, jugées par les mêmes règles que le placement lui-même.
    public IReadOnlyList<Coordinate> ValidOrigins(int length, Orientation orientation)
    {
        if (!remaining.Contains(length))
            return [];

        var occupied = ships.SelectMany(ship => ship.Cells).ToHashSet();
        return
        [
            .. from row in Enumerable.Range(0, Height)
               from column in Enumerable.Range(0, Width)
               let origin = new Coordinate(column, row)
               where PlacementRules.Check(PlacementRules.Cells(origin, length, orientation), Width, Height, occupied) is null
               select origin
        ];
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
