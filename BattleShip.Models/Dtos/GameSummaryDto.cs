namespace BattleShip.Models.Dtos;

public sealed record GameSummaryDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    GamePhase Phase,
    Side? Winner,
    int PlayerShotCount,
    int? DurationSeconds);
