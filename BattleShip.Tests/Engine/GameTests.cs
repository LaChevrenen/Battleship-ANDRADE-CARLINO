using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class GameTests
{
    private static readonly Coordinate Water = new(9, 9);

    // Même flotte réduite pour les deux camps : un navire en (0,0)-(1,0) et un navire en (5,5).
    private static Board SmallFleet() => new(10, 10, [new Ship([new(0, 0), new(1, 0)]), new Ship([new(5, 5)])]);

    private static Game NewGame(int seed) => new(SmallFleet(), SmallFleet(), new Random(seed));

    // Cherche une graine plutôt que de supposer comment Game traduit le tirage en camp.
    private static int SeedWhereFirstShooterIs(Side firstShooter)
    {
        for (var seed = 0; seed < 100; seed++)
        {
            var probe = new ScriptedTargets(Water);
            NewGame(seed).TryStart(probe.Next, out _);

            var computerStarted = probe.Calls > 0;
            if (computerStarted == (firstShooter == Side.Computer))
                return seed;
        }

        Assert.Fail($"Aucune graine sur 100 ne fait commencer {firstShooter}.");
        return -1;
    }

    private static Game GameStartedByPlayer()
    {
        var game = NewGame(SeedWhereFirstShooterIs(Side.Player));
        Assert.True(game.TryStart(new ScriptedTargets().Next, out _));
        return game;
    }

    [Fact]
    public void Une_partie_creee_attend_son_demarrage_sans_tireur_designe()
    {
        var game = NewGame(0);

        Assert.Equal(GamePhase.Setup, game.Phase);
        Assert.Null(game.CurrentTurn);
        Assert.Null(game.Winner);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Les_deux_camps_recoivent_chacun_la_flotte_par_defaut(int seed)
    {
        var game = Game.CreateWithRandomFleets(new Random(seed));

        int[] expected = [.. GameRules.DefaultShipLengths.Order()];
        Assert.Equal(expected, game.PlayerBoard.Ships.Select(ship => ship.Cells.Count).Order());
        Assert.Equal(expected, game.ComputerBoard.Ships.Select(ship => ship.Cells.Count).Order());
        Assert.NotSame(game.PlayerBoard, game.ComputerBoard);
    }

    [Fact]
    public void Le_premier_tireur_est_tire_au_sort_et_chaque_camp_peut_commencer()
    {
        Assert.InRange(SeedWhereFirstShooterIs(Side.Player), 0, 99);
        Assert.InRange(SeedWhereFirstShooterIs(Side.Computer), 0, 99);
    }

    [Fact]
    public void Demarrer_une_partie_deja_demarree_est_refuse()
    {
        var game = GameStartedByPlayer();

        Assert.False(game.TryStart(new ScriptedTargets().Next, out var computerShots));
        Assert.Empty(computerShots);
        Assert.Equal(Side.Player, game.CurrentTurn);
    }

    [Fact]
    public void L_ordinateur_qui_commence_joue_sa_serie_puis_rend_la_main()
    {
        var game = NewGame(SeedWhereFirstShooterIs(Side.Computer));
        var computer = new ScriptedTargets(new(5, 5), Water);

        Assert.True(game.TryStart(computer.Next, out var computerShots));

        Assert.Equal<ShotOutcome?>([ShotOutcome.Sunk, ShotOutcome.Miss], computerShots.Select(shot => shot.Result.Outcome));
        Assert.Equal(Side.Player, game.CurrentTurn);
    }

    [Fact]
    public void Un_tir_avant_le_demarrage_est_refuse_et_l_ordinateur_ne_joue_pas()
    {
        var game = NewGame(0);

        var turn = game.PlayerFire(new(0, 0), new ScriptedTargets().Next);

        Assert.Equal(ShotResult.Rejected(ShotRejection.NotStarted), turn.PlayerShot);
        Assert.Empty(turn.ComputerShots);
        Assert.Equal(GamePhase.Setup, game.Phase);
        Assert.Null(game.CurrentTurn);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(0, -1)]
    public void Un_tir_hors_grille_est_refuse_sans_rien_changer_et_l_ordinateur_ne_joue_pas(int column, int row)
    {
        var game = GameStartedByPlayer();

        var turn = game.PlayerFire(new(column, row), new ScriptedTargets().Next);

        Assert.Equal(ShotResult.Rejected(ShotRejection.OutOfBounds), turn.PlayerShot);
        Assert.Empty(turn.ComputerShots);
        Assert.Equal(Side.Player, game.CurrentTurn);
        Assert.Equal(GamePhase.InProgress, game.Phase);
        Assert.Null(game.Winner);
    }

    [Fact]
    public void Un_tir_sur_une_case_deja_tiree_est_refuse_sans_rien_changer_et_l_ordinateur_ne_joue_pas()
    {
        var game = GameStartedByPlayer();
        game.PlayerFire(new(0, 0), new ScriptedTargets().Next);

        var turn = game.PlayerFire(new(0, 0), new ScriptedTargets().Next);

        Assert.Equal(ShotResult.Rejected(ShotRejection.AlreadyTargeted), turn.PlayerShot);
        Assert.Empty(turn.ComputerShots);
        Assert.Equal(Side.Player, game.CurrentTurn);
        Assert.Equal(GamePhase.InProgress, game.Phase);
        // La série du joueur n'est pas interrompue : le segment restant coule toujours le navire.
        Assert.Equal(ShotOutcome.Sunk, game.PlayerFire(new(1, 0), new ScriptedTargets().Next).PlayerShot.Outcome);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void Un_tir_qui_touche_ou_coule_garde_la_main_et_l_ordinateur_ne_joue_pas(int column, int row)
    {
        var game = GameStartedByPlayer();

        var turn = game.PlayerFire(new(column, row), new ScriptedTargets().Next);

        Assert.True(turn.PlayerShot.IsAccepted);
        Assert.Empty(turn.ComputerShots);
        Assert.Equal(Side.Player, game.CurrentTurn);
        Assert.Equal(GamePhase.InProgress, game.Phase);
    }

    [Fact]
    public void Un_tir_a_l_eau_passe_la_main_a_l_ordinateur_qui_joue_jusqu_a_rater()
    {
        var game = GameStartedByPlayer();
        var computer = new ScriptedTargets(new(0, 0), Water);

        var turn = game.PlayerFire(Water, computer.Next);

        Assert.Equal(ShotResult.Miss, turn.PlayerShot);
        Assert.Equal<ShotOutcome?>([ShotOutcome.Hit, ShotOutcome.Miss], turn.ComputerShots.Select(shot => shot.Result.Outcome));
        Assert.Equal(Side.Player, game.CurrentTurn);
    }

    [Fact]
    public void L_ordinateur_subit_les_memes_refus_que_le_joueur()
    {
        var game = GameStartedByPlayer();
        var computer = new ScriptedTargets(new(0, 0), new(0, 0), new(10, 0), Water);

        var turn = game.PlayerFire(Water, computer.Next);

        Assert.Equal(4, computer.Calls);
        Assert.Equal<Coordinate>([new(0, 0), Water], turn.ComputerShots.Select(shot => shot.Target));
        Assert.Equal(Side.Player, game.CurrentTurn);
    }

    [Fact]
    public void Le_tir_qui_coule_le_dernier_navire_termine_la_partie_malgre_le_rejeu()
    {
        var game = GameStartedByPlayer();
        game.PlayerFire(new(5, 5), new ScriptedTargets().Next);
        game.PlayerFire(new(0, 0), new ScriptedTargets().Next);

        var winningTurn = game.PlayerFire(new(1, 0), new ScriptedTargets().Next);

        Assert.Equal(ShotOutcome.Sunk, winningTurn.PlayerShot.Outcome);
        Assert.Empty(winningTurn.ComputerShots);
        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Player, game.Winner);
        Assert.Null(game.CurrentTurn);

        var afterVictory = game.PlayerFire(Water, new ScriptedTargets().Next);

        Assert.Equal(ShotResult.Rejected(ShotRejection.GameOver), afterVictory.PlayerShot);
        Assert.Equal(Side.Player, game.Winner);
    }

    [Fact]
    public void L_ordinateur_qui_coule_le_dernier_navire_gagne_et_s_arrete_aussitot()
    {
        var game = GameStartedByPlayer();
        var computer = new ScriptedTargets(new(5, 5), new(0, 0), new(1, 0), Water);

        game.PlayerFire(Water, computer.Next);

        Assert.Equal(3, computer.Calls);
        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Computer, game.Winner);
        Assert.Null(game.CurrentTurn);
        Assert.Equal(
            ShotResult.Rejected(ShotRejection.GameOver),
            game.PlayerFire(new(0, 0), new ScriptedTargets().Next).PlayerShot);
    }

    private sealed class ScriptedTargets(params Coordinate[] targets)
    {
        private readonly Queue<Coordinate> remaining = new(targets);

        public int Calls { get; private set; }

        public Coordinate Next()
        {
            Calls++;
            Assert.True(remaining.Count > 0, "L'ordinateur a joué un coup non prévu par le scénario.");
            return remaining.Dequeue();
        }
    }
}
