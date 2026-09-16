namespace BattleShip.Models.Dtos;

// Grille du joueur : ses propres navires en clair, les tirs que l'ordinateur y a faits,
// et les longueurs qu'il lui reste à poser pendant la préparation.
public sealed record OwnBoardDto(
    int Width,
    int Height,
    IReadOnlyList<IReadOnlyList<Coordinate>> Ships,
    IReadOnlyList<Coordinate> Misses,
    IReadOnlyList<Coordinate> Hits,
    IReadOnlyList<int> RemainingShipLengths);
