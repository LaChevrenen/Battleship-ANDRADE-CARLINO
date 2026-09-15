using System.Collections.Frozen;
using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed class Ship(IEnumerable<Coordinate> cells)
{
    public IReadOnlySet<Coordinate> Cells { get; } = cells.ToFrozenSet();

    public bool Occupies(Coordinate cell) => Cells.Contains(cell);
}
