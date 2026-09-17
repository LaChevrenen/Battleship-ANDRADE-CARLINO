using BattleShip.Models;

namespace BattleShip.API.Engine;

// Une case réellement résolue par une attaque spéciale, avec son résultat individuel.
public sealed record AreaShot(Coordinate Target, ShotResult Result);

// Résultat d'une attaque spéciale côté tireur : la case visée se comporte comme le tir normal
// qu'elle remplace pour les refus (Rejection non nul, Cells vide, rien n'a été joué). Acceptée,
// elle détaille chaque case réellement résolue — la case visée, plus celles de ses voisines
// directes qui n'étaient ni hors grille ni déjà tirées.
public sealed record AreaShotResult(ShotResult Center, IReadOnlyList<AreaShot> Cells)
{
    public bool IsAccepted => Center.IsAccepted;
}
