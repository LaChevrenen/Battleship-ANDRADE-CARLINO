using BattleShip.Models.Dtos;

namespace BattleShip.App.Services;

// Un tir refusé n'a pas de tour à afficher : il porte seulement le motif renvoyé par le serveur.
// MustReload signale une version dépassée, seul cas où l'écran doit relire l'état avant de rejouer.
public sealed record FireResult(TurnDto? Turn, string? Refusal, bool MustReload);
