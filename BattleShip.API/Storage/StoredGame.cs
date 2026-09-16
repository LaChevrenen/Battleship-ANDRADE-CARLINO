using System.Diagnostics.CodeAnalysis;
using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.API.Storage;

// La version appartient au stockage, pas au moteur : elle sert à refuser un tir envoyé depuis un état dépassé.
public sealed class StoredGame(Guid id, Game game)
{
    public Guid Id { get; } = id;
    public Game Game { get; } = game;
    public int Version { get; private set; }

    public StartRejection? TryStart(Func<RevealedBoard, Coordinate> chooseComputerTarget, out IReadOnlyList<ComputerShot> computerShots)
    {
        var rejection = Game.TryStart(chooseComputerTarget, out computerShots);
        if (rejection is null)
            Version++;

        return rejection;
    }

    // Les trois opérations de préparation suivent la même règle que le tir : version vérifiée d'abord,
    // version augmentée seulement si la partie a réellement changé.
    public bool TryPlaceShip(
        Coordinate origin,
        int length,
        Orientation orientation,
        int expectedVersion,
        out PlacementRejection? rejection)
    {
        rejection = null;
        if (expectedVersion != Version)
            return false;

        rejection = Game.TryPlaceShip(origin, length, orientation);
        if (rejection is null)
            Version++;

        return true;
    }

    public bool TryRemoveLastShip(int expectedVersion, out bool removed)
    {
        removed = false;
        if (expectedVersion != Version)
            return false;

        removed = Game.TryRemoveLastShip();
        if (removed)
            Version++;

        return true;
    }

    public bool TryPlaceFleetAtRandom(int expectedVersion, out bool placed)
    {
        placed = false;
        if (expectedVersion != Version)
            return false;

        placed = Game.TryPlaceFleetAtRandom();
        if (placed)
            Version++;

        return true;
    }

    public bool TryFire(
        Coordinate target,
        int expectedVersion,
        Func<RevealedBoard, Coordinate> chooseComputerTarget,
        [NotNullWhen(true)] out PlayerTurnResult? turn)
    {
        if (expectedVersion != Version)
        {
            turn = null;
            return false;
        }

        turn = Game.PlayerFire(target, chooseComputerTarget);
        // Un tir refusé ne change pas la partie, donc pas la version.
        if (turn.PlayerShot.IsAccepted)
            Version++;

        return true;
    }
}
