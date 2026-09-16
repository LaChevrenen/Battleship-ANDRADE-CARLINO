namespace BattleShip.App.Services;

// Quelle longueur reste choisie quand la flotte en préparation change.
public static class ShipSelection
{
    // Une absence de sélection doit rester une absence de sélection, notamment après le
    // déplacement d'un bateau déjà posé.
    public static int? Keep(int? current, IReadOnlyList<int> remaining) =>
        current is { } length && remaining.Contains(length) ? length : null;
}
