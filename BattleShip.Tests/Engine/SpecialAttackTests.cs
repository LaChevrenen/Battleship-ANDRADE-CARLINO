using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class SpecialAttackTests
{
    // Même technique que GameTests.SeedWhereFirstShooterIs : le tirage au sort du premier tireur
    // ne dépend que du Random, jamais de la flotte, donc la même recherche s'applique ici.
    private static Game GameStartedByPlayer(IEnumerable<Ship> computerFleet) =>
        GameStartedByPlayer(computerFleet, new Ship([new(9, 9)]));

    private static Game GameStartedByPlayer(IEnumerable<Ship> computerFleet, Ship playerShip)
    {
        var fleet = computerFleet.ToList();
        for (var seed = 0; seed < 100; seed++)
        {
            var game = new Game(FleetUnderConstruction.Placed(10, 10, [playerShip]), new Board(10, 10, fleet), new Random(seed));
            var probe = new ScriptedTargets(new Coordinate(9, 9));
            game.TryStart(probe.Next, out _);
            if (probe.Calls == 0)
                return game;
        }

        Assert.Fail("Aucune graine sur 100 ne fait commencer le joueur.");
        return null!;
    }

    // Charge la jauge du joueur à bloc par cinq tirs manqués de chaque côté, alternés. Les cases
    // passées ici sont les cases que le JOUEUR vise (sur la grille adverse) ; l'ordinateur, lui,
    // manque toujours en (0, row) sur la grille du joueur (seul son navire est en (9, 9)).
    private static void ChargePlayer(Game game, params Coordinate[] playerMisses)
    {
        Assert.Equal(GameRules.SpecialAttackChargeInterval, playerMisses.Length);

        for (var row = 0; row < playerMisses.Length; row++)
        {
            var computerMiss = new ScriptedTargets(new Coordinate(0, row));
            game.PlayerFire(playerMisses[row], computerMiss.Next);
        }
    }

    [Fact]
    public void Cinq_tirs_manques_chargent_l_attaque_speciale_pas_avant()
    {
        var game = GameStartedByPlayer([new Ship([new(5, 5)])]);
        Coordinate[] misses = [new(9, 0), new(9, 1), new(9, 2), new(9, 3), new(9, 4)];

        for (var row = 0; row < 4; row++)
        {
            var computerMiss = new ScriptedTargets(new Coordinate(0, row));
            game.PlayerFire(misses[row], computerMiss.Next);
            Assert.False(game.HasSpecialAttackCharge(Side.Player));
        }

        var lastComputerMiss = new ScriptedTargets(new Coordinate(0, 4));
        game.PlayerFire(misses[4], lastComputerMiss.Next);

        Assert.True(game.HasSpecialAttackCharge(Side.Player));
    }

    [Fact]
    public void Une_attaque_speciale_sans_charge_est_refusee_sans_rien_changer()
    {
        var game = GameStartedByPlayer([new Ship([new(5, 5)])]);

        var result = game.PlayerFireSpecialAttack(new(5, 5), new ScriptedTargets().Next);

        Assert.Equal(ShotRejection.SpecialAttackNotCharged, result.PlayerShot.Center.Rejection);
        Assert.False(result.PlayerShot.IsAccepted);
        Assert.Empty(result.PlayerShot.Cells);
        Assert.Empty(result.ComputerShots);
        Assert.Equal(Side.Player, game.CurrentTurn);
        // Aucune trace : ni la case visée ni ses voisines ne sont devenues des cases tirées.
        // RevealedBoard est un record dont les collections se comparent par référence : on
        // regarde le contenu case par case plutôt que l'égalité du record entier.
        var after = game.ComputerBoard.Reveal();
        Assert.Empty(after.Misses);
        Assert.Empty(after.Hits);
        Assert.Empty(after.SunkShips);
    }

    [Fact]
    public void Une_attaque_speciale_touche_la_case_visee_et_ses_quatre_voisines()
    {
        // Navire de 3 cases traversé par la croix (centre + gauche + droite), et un second navire
        // ailleurs pour que le couler ne termine pas la partie : on peut alors observer le rejeu.
        var game = GameStartedByPlayer([new Ship([new(5, 5), new(6, 5), new(7, 5)]), new Ship([new(0, 0)])]);
        ChargePlayer(game, new(9, 0), new(9, 1), new(9, 2), new(9, 3), new(9, 4));

        var result = game.PlayerFireSpecialAttack(new(6, 5), new ScriptedTargets().Next);

        Assert.True(result.PlayerShot.IsAccepted);
        var cells = result.PlayerShot.Cells;
        Assert.Equal(5, cells.Count);
        Assert.Equal<Coordinate>([new(6, 5), new(5, 5), new(7, 5), new(6, 4), new(6, 6)], cells.Select(cell => cell.Target));

        // Le navire de 3 cases est coulé (ses trois cases sont dans la croix), l'autre ne l'est pas.
        Assert.Equal(1, cells.Count(cell => cell.Result.Outcome == ShotOutcome.Sunk));
        Assert.Equal(2, cells.Count(cell => cell.Result.Outcome == ShotOutcome.Hit));
        Assert.Equal(2, cells.Count(cell => cell.Result.Outcome == ShotOutcome.Miss));

        // Au moins une case touche : la partie continue, et le tireur garde la main.
        Assert.Equal(GamePhase.InProgress, game.Phase);
        Assert.Equal(Side.Player, game.CurrentTurn);
        // Consommée, pas comptée comme un tir de plus : la jauge repart de zéro.
        Assert.Equal(0, game.SpecialAttackProgress(Side.Player));
    }

    [Fact]
    public void Une_attaque_speciale_qui_coule_le_dernier_navire_termine_la_partie()
    {
        // Un seul navire sur toute la grille : le couler met immédiatement fin à la partie.
        var game = GameStartedByPlayer([new Ship([new(5, 5), new(6, 5), new(7, 5)])]);
        ChargePlayer(game, new(9, 0), new(9, 1), new(9, 2), new(9, 3), new(9, 4));

        var result = game.PlayerFireSpecialAttack(new(6, 5), new ScriptedTargets().Next);

        Assert.Contains(result.PlayerShot.Cells, cell => cell.Result.Outcome == ShotOutcome.Sunk);
        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Player, game.Winner);
        Assert.Null(game.CurrentTurn);
    }

    [Fact]
    public void Une_attaque_speciale_ignore_les_voisines_hors_grille_ou_deja_tirees()
    {
        var game = GameStartedByPlayer([new Ship([new(9, 8)])]);
        // (1, 0) est délibérément l'une des cinq cases de charge : elle sera déjà tirée avant
        // l'attaque spéciale. ChargePlayer fait aussi jouer l'ordinateur cinq fois (un tir par
        // tour rendu) : sa propre jauge est donc, elle aussi, chargée à la fin de cette phase.
        ChargePlayer(game, new(1, 0), new(5, 1), new(5, 2), new(5, 3), new(5, 4));
        Assert.True(game.HasSpecialAttackCharge(Side.Computer));

        // Croix autour de (0, 0) : deux voisines hors grille (-1,0) et (0,-1), et (1, 0) déjà tirée.
        // Seules (0, 0) et (0, 1) restent à résoudre.
        var result = game.PlayerFireSpecialAttack(new(0, 0), new ScriptedTargets(new Coordinate(0, 5)).Next);

        Assert.True(result.PlayerShot.IsAccepted);
        Assert.Equal<Coordinate>([new(0, 0), new(0, 1)], result.PlayerShot.Cells.Select(cell => cell.Target));
        Assert.All(result.PlayerShot.Cells, cell => Assert.Equal(ShotOutcome.Miss, cell.Result.Outcome));

        // Aucune case touchée par le joueur : le tour passe à l'ordinateur, qui — chargé lui
        // aussi — riposte automatiquement par sa propre attaque spéciale plutôt qu'un tir simple.
        // Sa croix (centre (0,5), voisines (-1,5) hors grille, (1,5), (0,4) déjà tirée par lui
        // pendant la charge, (0,6)) ignore les mêmes deux catégories de cases que celle du joueur.
        Assert.Equal(3, result.ComputerShots.Count);
        Assert.Equal<Coordinate>([new(0, 5), new(1, 5), new(0, 6)], result.ComputerShots.Select(shot => shot.Target));
        Assert.All(result.ComputerShots, shot => Assert.Equal(ShotOutcome.Miss, shot.Result.Outcome));

        // Cette seconde attaque spéciale manque aussi entièrement : le tour revient au joueur, et
        // les deux jauges, consommées chacune une fois, repartent de zéro.
        Assert.Equal(Side.Player, game.CurrentTurn);
        Assert.Equal(0, game.SpecialAttackProgress(Side.Player));
        Assert.Equal(0, game.SpecialAttackProgress(Side.Computer));
    }

    [Fact]
    public void L_ordinateur_utilise_son_attaque_speciale_des_qu_il_est_charge()
    {
        // Le joueur rate cinq fois d'affilée ; l'ordinateur aussi, pour ne rien couler avant
        // l'heure. Le sixième tir du joueur manque encore : c'est cette fois l'ordinateur qui
        // riposte chargé, sa croix centrée sur la case scriptée doit toucher les trois cases
        // du navire du joueur alignées autour d'elle.
        var playerShip = new Ship([new(4, 4), new(5, 4), new(6, 4)]);
        var game = GameStartedByPlayer([new Ship([new(9, 9)])], playerShip);

        // Cinq tirs manqués de chaque côté : les deux jauges se chargent en même temps, puisque
        // chaque tour joué (l'un puis l'autre) avance les deux compteurs alternativement.
        Coordinate[] playerTargets = [new(9, 0), new(9, 1), new(9, 2), new(9, 3), new(9, 5)];
        for (var row = 0; row < 5; row++)
        {
            var computerMiss = new ScriptedTargets(new Coordinate(0, row));
            game.PlayerFire(playerTargets[row], computerMiss.Next);
        }

        Assert.True(game.HasSpecialAttackCharge(Side.Computer));

        // Sixième tir du joueur, manqué : le tour passe, et l'ordinateur riposte chargé avec sa
        // croix centrée sur (5, 4), qui touche les trois cases du navire aligné.
        var computerSpecialAttack = new ScriptedTargets(new Coordinate(5, 4));
        var result = game.PlayerFire(new(9, 6), computerSpecialAttack.Next);

        Assert.True(result.ComputerShots.Count >= 3);
        Assert.Contains(result.ComputerShots, shot => shot.Target == new Coordinate(4, 4) && shot.Result.Outcome != ShotOutcome.Miss);
        Assert.Contains(result.ComputerShots, shot => shot.Target == new Coordinate(6, 4) && shot.Result.Outcome != ShotOutcome.Miss);
        Assert.Equal(0, game.SpecialAttackProgress(Side.Computer));
        // Le navire du joueur n'a que ces trois cases : les couler d'un coup termine la partie.
        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Computer, game.Winner);
    }

    private sealed class ScriptedTargets(params Coordinate[] targets)
    {
        private readonly Queue<Coordinate> remaining = new(targets);

        public int Calls { get; private set; }

        public Coordinate Next(RevealedBoard view)
        {
            Calls++;
            Assert.True(remaining.Count > 0, "L'ordinateur a joué un coup non prévu par le scénario.");
            return remaining.Dequeue();
        }
    }
}
