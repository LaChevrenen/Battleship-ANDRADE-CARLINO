using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed record RevealedBoard(
    int Width,
    int Height,
    IReadOnlySet<Coordinate> Misses,
    IReadOnlySet<Coordinate> Hits,
    IReadOnlyList<IReadOnlySet<Coordinate>> SunkShips);
