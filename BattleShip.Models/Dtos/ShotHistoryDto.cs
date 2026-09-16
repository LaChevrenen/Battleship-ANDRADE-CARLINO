namespace BattleShip.Models.Dtos;

public sealed record ShotHistoryDto(Coordinate Target, ShotOutcome Outcome);