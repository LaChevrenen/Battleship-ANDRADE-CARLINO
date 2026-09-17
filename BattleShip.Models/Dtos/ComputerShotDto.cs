namespace BattleShip.Models.Dtos;

public sealed record ComputerShotDto(Coordinate Target, ShotOutcome Outcome, bool SpecialAttack = false);
