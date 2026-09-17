namespace BattleShip.API.Engine;

public static class GameRules
{
    public const int GridSize = 10;
    public const int MaxPlacementAttempts = 1000;

    // Bornes d'une partie personnalisée : docs/REGLES.md.
    public const int MinGridSize = 5;
    public const int MaxGridSize = 15;
    public const int MinShipLength = 1;
    public const int MaxShipLength = 5;
    public const int MaxShipsPerLength = 3;

    public static IReadOnlyList<int> DefaultShipLengths { get; } = [5, 4, 3, 3, 2];
}
