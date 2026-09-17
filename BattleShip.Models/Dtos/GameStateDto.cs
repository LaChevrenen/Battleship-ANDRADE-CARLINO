namespace BattleShip.Models.Dtos;

public sealed record GameStateDto(
    Guid Id,
    int Version,
    GamePhase Phase,
    Side? CurrentTurn,
    Side? Winner,
    OwnBoardDto Player,
    OpponentBoardDto Opponent,
    GameStatisticsDto Statistics,
    AiDifficulty Difficulty);
