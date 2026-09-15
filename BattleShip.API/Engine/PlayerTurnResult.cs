namespace BattleShip.API.Engine;

public sealed record PlayerTurnResult(ShotResult PlayerShot, IReadOnlyList<ComputerShot> ComputerShots);
