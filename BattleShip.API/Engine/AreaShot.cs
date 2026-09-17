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

    // Résumé retenu partout où une attaque spéciale doit se lire comme un seul tir — historique,
    // statistiques, contrat HTTP : un navire coulé parmi les cases visées l'emporte sur une simple
    // touche, qui l'emporte sur un tir à l'eau. Une seule source de vérité pour ce calcul.
    public ShotOutcome? AggregateOutcome =>
        Cells.Count == 0 ? null
        : Cells.Any(cell => cell.Result.Outcome == ShotOutcome.Sunk) ? ShotOutcome.Sunk
        : Cells.Any(cell => cell.Result.Outcome == ShotOutcome.Hit) ? ShotOutcome.Hit
        : ShotOutcome.Miss;

    // Si plusieurs navires coulent dans la même attaque (possible si le contact est autorisé),
    // seul le premier rencontré est retenu ici : même limite qu'un tir simple, qui n'en annonce
    // jamais qu'un à la fois.
    public Ship? SunkShip => Cells.FirstOrDefault(cell => cell.Result.Outcome == ShotOutcome.Sunk)?.Result.SunkShip;
}
