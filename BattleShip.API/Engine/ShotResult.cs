using BattleShip.Models;

namespace BattleShip.API.Engine;

// Constructeur privé : un résultat est soit accepté (Outcome), soit refusé (Rejection), jamais les deux.
public sealed record ShotResult
{
    private ShotResult(ShotOutcome? outcome, ShotRejection? rejection, Ship? sunkShip)
    {
        Outcome = outcome;
        Rejection = rejection;
        SunkShip = sunkShip;
    }

    public ShotOutcome? Outcome { get; }
    public ShotRejection? Rejection { get; }
    public Ship? SunkShip { get; }

    public bool IsAccepted => Rejection is null;

    public static ShotResult Miss { get; } = new(ShotOutcome.Miss, null, null);
    public static ShotResult Hit { get; } = new(ShotOutcome.Hit, null, null);
    public static ShotResult Sunk(Ship ship) => new(ShotOutcome.Sunk, null, ship);
    public static ShotResult Rejected(ShotRejection reason) => new(null, reason, null);
}
