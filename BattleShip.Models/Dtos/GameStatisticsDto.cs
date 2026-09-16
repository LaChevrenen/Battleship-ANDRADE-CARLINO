namespace BattleShip.Models.Dtos;

public sealed record GameStatisticsDto(
    int TotalShots,
    int SuccessfulShots,
    int MissedShots,
    int AccuracyPercentage,
    int? DurationSeconds,
    IReadOnlyList<ShotHistoryDto> History);