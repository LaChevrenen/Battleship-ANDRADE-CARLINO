namespace BattleShip.Models;

public enum ShotRejection
{
    OutOfBounds,
    AlreadyTargeted,
    NotYourTurn,
    NotStarted,
    GameOver
}
