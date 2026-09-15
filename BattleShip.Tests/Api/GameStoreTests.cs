using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;

namespace BattleShip.Tests.Api;

public sealed class GameStoreTests
{
    private static Coordinate ChooseComputerTarget(RevealedBoard view) =>
        HuntTargetStrategy.ChooseTarget(view, Random.Shared);

    [Fact]
    public void Une_partie_ajoutee_est_retrouvee_par_son_identifiant()
    {
        var store = new GameStore();
        var game = Game.CreateWithRandomFleets(new Random(0));
        var id = store.Add(game, stored => stored.Id);

        Assert.True(store.TryExecute(id, stored => stored.Game, out var found));

        Assert.Same(game, found);
    }

    [Fact]
    public void Une_partie_inconnue_n_est_pas_trouvee_et_l_operation_n_est_pas_executee()
    {
        var store = new GameStore();
        var executed = false;

        var found = store.TryExecute(Guid.NewGuid(), _ => executed = true, out _);

        Assert.False(found);
        Assert.False(executed);
    }

    [Fact]
    public async Task Des_tirs_simultanes_sur_une_meme_partie_sont_juges_un_par_un()
    {
        // Test probabiliste : sans verrou, la course peut ne pas se produire pendant cette exécution.
        var store = new GameStore();
        var id = store.Add(Game.CreateWithRandomFleets(new Random(0)), stored => stored.Id);
        store.TryExecute(id, stored => stored.TryStart(ChooseComputerTarget, out _), out _);
        var accepted = 0;

        await Parallel.ForEachAsync(Enumerable.Range(0, 100), (i, _) =>
        {
            var cell = new Coordinate(i % 10, i / 10);
            store.TryExecute(
                id,
                stored => stored.TryFire(cell, stored.Version, ChooseComputerTarget, out var turn) && turn.PlayerShot.IsAccepted,
                out var shotAccepted);
            if (shotAccepted)
                Interlocked.Increment(ref accepted);
            return ValueTask.CompletedTask;
        });

        store.TryExecute(id, stored => stored.Version, out var version);
        // Démarrage + un incrément par tir accepté : une écriture concurrente perdrait des incréments ou lèverait une exception.
        Assert.Equal(accepted + 1, version);
    }
}
