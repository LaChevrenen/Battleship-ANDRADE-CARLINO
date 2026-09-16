namespace BattleShip.API.Engine;

public sealed record PlacementResult(IReadOnlyList<Ship>? Ships, int AttemptsUsed);
