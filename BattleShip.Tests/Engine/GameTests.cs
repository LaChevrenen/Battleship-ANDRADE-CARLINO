using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class GameTests
{
    private static readonly Coordinate Water = new(9, 9);

    // Même flotte réduite pour les deux camps : un navire en (0,0)-(1,0) et un navire en (5,5).
    private static Ship[] SmallFleet() => [new Ship([new(0, 0), new(1, 0)]), new Ship([new(5, 5)])];

    private static Game NewGame(int seed) =>
        new(FleetUnderConstruction.Placed(10, 10, SmallFleet()), new Board(10, 10, SmallFleet()), new Random(seed));

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
        Assert.Null(game.TryStart(new ScriptedTargets().Next, out _));
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
    public void Une_partie_creee_place_la_flotte_de_l_ordinateur_et_laisse_celle_du_joueur_a_poser(int seed)
    {
        var game = Game.CreateWithRandomComputerFleet(new Random(seed));

        int[] expected = [.. GameRules.DefaultShipLengths.Order()];
        Assert.Equal(expected, game.ComputerBoard.Ships.Select(ship => ship.Cells.Count).Order());
        Assert.Empty(game.PlayerBoard.Ships);
        // Même flotte pour les deux camps : celle du joueur reste entièrement à poser.
        Assert.Equal(expected, game.RemainingShipLengths.Order());
    }

    [Fact]
    public void Le_premier_tireur_est_tire_au_sort_et_chaque_camp_peut_commencer()
    {
        Assert.InRange(SeedWhereFirstShooterIs(Side.Player), 0, 99);
        Assert.InRange(SeedWhereFirstShooterIs(Side.Computer), 0, 99);
    }

    public static IEnumerable<object[]> Seeds => Enumerable.Range(0, 20).Select(seed => new object[] { seed });

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Demarrer_une_partie_deja_demarree_est_refuse_sans_changer_le_tour_ni_les_grilles(int seed)
    {
        // Plusieurs graines : un second tirage au sort fautif finit par désigner l'ordinateur.
        var game = NewGame(seed);
        game.TryStart(new ScriptedTargets(Water).Next, out _);
        game.PlayerFire(new(0, 0), new ScriptedTargets().Next);
        var playerBoard = game.PlayerBoard;
        var computerBoard = game.ComputerBoard;

        Assert.Equal(StartRejection.AlreadyStarted, game.TryStart(new ScriptedTargets().Next, out var computerShots));

        Assert.Empty(computerShots);
        Assert.Equal(GamePhase.InProgress, game.Phase);
        Assert.Equal(Side.Player, game.CurrentTurn);
        Assert.Same(playerBoard, game.PlayerBoard);
        Assert.Same(computerBoard, game.ComputerBoard);
        // Le tir d'avant le second démarrage est toujours enregistré.
        Assert.Equal(
            ShotResult.Rejected(ShotRejection.AlreadyTargeted),
            game.PlayerFire(new(0, 0), new ScriptedTargets().Next).PlayerShot);
    }

    [Fact]
    public void L_ordinateur_qui_commence_joue_sa_serie_puis_rend_la_main()
    {
        var game = NewGame(SeedWhereFirstShooterIs(Side.Computer));
        var computer = new ScriptedTargets(new(5, 5), Water);

        Assert.Null(game.TryStart(computer.Next, out var computerShots));

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

    [Fact]
    public void L_ordinateur_choisit_sa_case_a_partir_de_la_vue_de_la_grille_du_joueur()
    {
        var game = GameStartedByPlayer();
        var computer = new ScriptedTargets(new(0, 0), Water);

        game.PlayerFire(Water, computer.Next);

        var firstView = computer.Views[0];
        // Le raté du joueur est sur la grille de l'ordinateur : il ne doit pas apparaître ici.
        Assert.Empty(firstView.Misses);
        Assert.Empty(firstView.Hits);
        Assert.Equal((10, 10), (firstView.Width, firstView.Height));
        Assert.Equivalent(new[] { new Coordinate(0, 0) }, computer.Views[1].Hits, strict: true);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void L_ordinateur_branche_sur_la_chasse_cible_gagne_une_partie_complete(int seed)
    {
        // Ce test boucle dans Game si la stratégie propose une case refusée : il ne vient qu'après le test de terminaison sur Board.
        var strategyRandom = new Random(seed);
        Coordinate ChooseComputerTarget(RevealedBoard view) => HuntTargetStrategy.ChooseTarget(view, strategyRandom);
        // Grille de l'ordinateur à un seul navire : le joueur a 99 cases d'eau, assez pour rendre la main à chaque tour.
        var computerShip = new Coordinate(9, 9);
        var game = new Game(
            FleetUnderConstruction.Placed(10, 10, SmallFleet()),
            new Board(10, 10, [new Ship([computerShip])]),
            new Random(seed));
        game.TryStart(ChooseComputerTarget, out _);

        var playerWater = Enumerable.Range(0, 100).Select(i => new Coordinate(i % 10, i / 10)).Where(cell => cell != computerShip);
        foreach (var cell in playerWater.TakeWhile(_ => game.Phase == GamePhase.InProgress))
            Assert.Equal(ShotResult.Miss, game.PlayerFire(cell, ChooseComputerTarget).PlayerShot);

        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Computer, game.Winner);
    }

    private sealed class ScriptedTargets(params Coordinate[] targets)
    {
        private readonly Queue<Coordinate> remaining = new(targets);

        public int Calls => Views.Count;

        public List<RevealedBoard> Views { get; } = [];

        public Coordinate Next(RevealedBoard view)
        {
            Views.Add(view);
            Assert.True(remaining.Count > 0, "L'ordinateur a joué un coup non prévu par le scénario.");
            return remaining.Dequeue();
        }
    }
}
