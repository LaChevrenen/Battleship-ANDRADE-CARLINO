using BattleShip.Models.Dtos;

namespace BattleShip.App.Services;

// Résultat d'un démarrage ou d'un tir. Un appel refusé n'a pas de tour à afficher : il porte
// seulement le motif renvoyé par le serveur. MustReload signale les cas où l'écran doit relire l'état.
public sealed record TurnResult(TurnDto? Turn, string? Refusal, bool MustReload);
