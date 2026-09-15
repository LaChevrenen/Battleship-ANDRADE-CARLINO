namespace BattleShip.Models.Dtos;

// Aucun champ ne peut porter un navire adverse non coulé : seuls les tirs du joueur et les navires qu'il a coulés.
public sealed record OpponentBoardDto(
    int Width,
    int Height,
    IReadOnlyList<Coordinate> Misses,
    IReadOnlyList<Coordinate> Hits,
    IReadOnlyList<IReadOnlyList<Coordinate>> SunkShips);
