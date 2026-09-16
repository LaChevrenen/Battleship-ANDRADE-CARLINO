namespace BattleShip.Models.Dtos;

// Grille du joueur : ses propres navires en clair, les tirs que l'ordinateur y a faits,
// ceux de ses navires qui sont coulés, et les longueurs qu'il lui reste à poser.
public sealed record OwnBoardDto(
    int Width,
    int Height,
    IReadOnlyList<IReadOnlyList<Coordinate>> Ships,
    IReadOnlyList<Coordinate> Misses,
    IReadOnlyList<Coordinate> Hits,
    IReadOnlyList<IReadOnlyList<Coordinate>> SunkShips,
    IReadOnlyList<int> RemainingShipLengths);
