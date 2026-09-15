namespace BattleShip.API.Engine;

public static class GameRules
{
    public const int GridSize = 10;
    public const int MaxPlacementAttempts = 1000;

    public static IReadOnlyList<int> DefaultShipLengths { get; } = [5, 4, 3, 3, 2];
}
