namespace BattleShip.Models.Dtos;

// PlayerOutcome et SunkShip sont absents au démarrage, où seul l'ordinateur a pu tirer.
public sealed record TurnDto(
    ShotOutcome? PlayerOutcome,
    IReadOnlyList<Coordinate>? SunkShip,
    IReadOnlyList<ComputerShotDto> ComputerShots,
    GameStateDto State);
