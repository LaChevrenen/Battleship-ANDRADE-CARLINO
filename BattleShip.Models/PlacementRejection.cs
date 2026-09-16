namespace BattleShip.Models;

public enum PlacementRejection
{
    OutOfBounds,
    Overlap,
    AdjacentShip,
    LengthNotAvailable,
    NotInSetup
}
