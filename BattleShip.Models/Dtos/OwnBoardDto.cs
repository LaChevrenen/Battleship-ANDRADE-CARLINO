namespace BattleShip.Models.Dtos;

// Grille du joueur : ses propres navires en clair, et les tirs que l'ordinateur y a faits.
public sealed record OwnBoardDto(
    int Width,
    int Height,
    IReadOnlyList<IReadOnlyList<Coordinate>> Ships,
    IReadOnlyList<Coordinate> Misses,
    IReadOnlyList<Coordinate> Hits);
