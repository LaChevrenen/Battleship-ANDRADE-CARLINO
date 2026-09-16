using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;

namespace BattleShip.Tests.Api;

public sealed class StoredGameTests
{
    private static readonly Coordinate Water = new(9, 9);

    private int computerCalls;

    private Coordinate ChooseComputerTarget(RevealedBoard view)
    {
        computerCalls++;
        return HuntTargetStrategy.ChooseTarget(view, new Random(computerCalls));
    }

    private static StoredGame NewStoredGame() =>
        new(Guid.NewGuid(), new Game(FleetUnderConstruction.Placed(10, 10, SmallFleet()), new Board(10, 10, SmallFleet()), new Random(0)));

    private static Ship[] SmallFleet() => [new Ship([new(0, 0), new(1, 0)])];

    private StoredGame StartedGame()
    {
        var stored = NewStoredGame();
        Assert.Null(stored.TryStart(ChooseComputerTarget, out _));
        computerCalls = 0;
        return stored;
    }

    [Fact]
    public void Une_partie_stockee_commence_a_la_version_zero()
    {
        Assert.Equal(0, NewStoredGame().Version);
    }

    [Fact]
    public void Un_demarrage_reussi_augmente_la_version_et_un_second_demarrage_ne_la_change_pas()
    {
        var stored = StartedGame();

        Assert.Equal(1, stored.Version);
        Assert.Equal(StartRejection.AlreadyStarted, stored.TryStart(ChooseComputerTarget, out _));
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public void Un_tir_accepte_augmente_la_version()
    {
        var stored = StartedGame();

        Assert.True(stored.TryFire(Water, expectedVersion: 1, ChooseComputerTarget, out var turn));

        Assert.True(turn.PlayerShot.IsAccepted);
        Assert.Equal(2, stored.Version);
    }

    [Fact]
    public void Un_tir_refuse_par_le_moteur_ne_change_pas_la_version()
    {
        var stored = StartedGame();

        Assert.True(stored.TryFire(new(10, 0), expectedVersion: 1, ChooseComputerTarget, out var turn));

        Assert.Equal(ShotRejection.OutOfBounds, turn.PlayerShot.Rejection);
        Assert.Equal(1, stored.Version);
    }

    [Fact]
    public void Un_tir_depuis_une_version_depassee_est_refuse_sans_toucher_la_partie()
    {
        var stored = StartedGame();

        Assert.False(stored.TryFire(Water, expectedVersion: 0, ChooseComputerTarget, out var turn));

        Assert.Null(turn);
        Assert.Equal(1, stored.Version);
        Assert.Equal(0, computerCalls);
        // Le tir refusé n'a pas été joué : la même case est encore libre.
        Assert.True(stored.TryFire(Water, expectedVersion: 1, ChooseComputerTarget, out var replay));
        Assert.True(replay.PlayerShot.IsAccepted);
    }
}
