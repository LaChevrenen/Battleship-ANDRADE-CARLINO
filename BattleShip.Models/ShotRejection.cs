namespace BattleShip.Models;

public enum ShotRejection
{
    OutOfBounds,
    AlreadyTargeted,
    NotYourTurn,
    NotStarted,
    GameOver,
    // La case visée par une attaque spéciale n'a rien à voir avec ce motif ; il ne dépend que de
    // la jauge du tireur, jamais de la case elle-même.
    SpecialAttackNotCharged
}
