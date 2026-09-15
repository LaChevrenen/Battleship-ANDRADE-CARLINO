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

    public bool TryStart(Func<RevealedBoard, Coordinate> chooseComputerTarget, out IReadOnlyList<ComputerShot> computerShots)
    {
        if (!Game.TryStart(chooseComputerTarget, out computerShots))
            return false;

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
