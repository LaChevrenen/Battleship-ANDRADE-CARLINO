namespace BattleShip.Models.Dtos;

// Aucun champ ne peut porter un navire adverse non coulé pendant que la partie se joue : seuls
// les tirs du joueur et les navires qu'il a coulés. Seule exception, volontaire : RemainingShips,
// qui ne se remplit que si la partie est terminée et perdue — voir GameDtoMapper.
public sealed record OpponentBoardDto(
    int Width,
    int Height,
    IReadOnlyList<Coordinate> Misses,
    IReadOnlyList<Coordinate> Hits,
    IReadOnlyList<IReadOnlyList<Coordinate>> SunkShips,
    // Navires adverses jamais coulés, révélés seulement en cas de défaite : la partie est finie,
    // le joueur ne peut plus rien en faire, il n'y a plus rien à protéger derrière ce masquage.
    IReadOnlyList<IReadOnlyList<Coordinate>> RemainingShips);
