using BattleShip.Models;

namespace BattleShip.API.Engine;

public sealed record RevealedBoard(
    IReadOnlySet<Coordinate> Misses,
    IReadOnlySet<Coordinate> Hits,
    IReadOnlyList<IReadOnlySet<Coordinate>> SunkShips);
