namespace BattleShip.Models;

public enum PlacementRejection
{
    OutOfBounds,
    Overlap,
    AdjacentShip,
    LengthNotAvailable,
    NoShipHere,
    NotInSetup
}
