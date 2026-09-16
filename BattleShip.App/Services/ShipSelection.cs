namespace BattleShip.App.Services;

// Quelle longueur reste choisie quand la flotte en préparation change.
public static class ShipSelection
{
    // Rend null sur une flotte complète. Un défaut d'entier — FirstOrDefault() sur une liste vide
    // rend 0, pas une absence — envoyait une longueur 0 au serveur, qui refuse en 400.
    public static int? Keep(int? current, IReadOnlyList<int> remaining) =>
        remaining.Count == 0 ? null
        : current is { } length && remaining.Contains(length) ? length
        : remaining.Min();
}
