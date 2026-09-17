namespace BattleShip.API.Engine;

public sealed record PlayerAreaTurnResult(AreaShotResult PlayerShot, IReadOnlyList<ComputerShot> ComputerShots);
