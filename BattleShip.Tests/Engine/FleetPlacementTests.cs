using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class FleetPlacementTests
{
    private static readonly int[] TwoShips = [3, 2];

    private static FleetUnderConstruction NewFleet() => new(10, 10, TwoShips);

    private static Game NewGame(IReadOnlyList<int> lengths, int seed = 0) =>
        new(new FleetUnderConstruction(10, 10, lengths),
            new Board(10, 10, [new Ship([new(9, 9)])]),
            new Random(seed));

    private static Coordinate ChooseComputerTarget(RevealedBoard view) => HuntTargetStrategy.ChooseTarget(view, new Random(0));

    // Cells est un ensemble, sans ordre garanti, et Coordinate n'implémente pas IComparable.
    private static IEnumerable<Coordinate> Ordered(IEnumerable<Coordinate> cells) =>
        cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column);

    [Fact]
    public void Une_flotte_neuve_attend_toutes_ses_longueurs()
    {
        var fleet = NewFleet();

        Assert.Equal(TwoShips, fleet.RemainingLengths);
        Assert.Empty(fleet.Ships);
        Assert.False(fleet.IsComplete);
    }

    [Fact]
    public void Un_navire_pose_retire_sa_longueur_des_restantes()
    {
        var fleet = NewFleet();

        Assert.Null(fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal));

        Assert.Equal<int>([2], fleet.RemainingLengths);
        Assert.Equal<Coordinate>([new(0, 0), new(1, 0), new(2, 0)], Assert.Single(fleet.Ships).Cells);
    }

    [Fact]
    public void Une_longueur_absente_des_restantes_est_refusee()
    {
        var fleet = NewFleet();

        Assert.Equal(PlacementRejection.LengthNotAvailable, fleet.TryPlace(new(0, 0), 4, Orientation.Horizontal));
        Assert.Empty(fleet.Ships);
    }

    [Fact]
    public void Un_navire_refuse_ne_change_rien_a_la_flotte()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.AdjacentShip, fleet.TryPlace(new(0, 1), 2, Orientation.Horizontal));

        Assert.Equal<int>([2], fleet.RemainingLengths);
        Assert.Single(fleet.Ships);
    }

    [Fact]
    public void Retirer_un_navire_par_une_de_ses_cases_rend_sa_longueur()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        // Une case du milieu : le joueur clique n'importe où sur le navire.
        Assert.True(fleet.TryRemoveAt(new(1, 0)));

        Assert.Equal<int>([2, 3], fleet.RemainingLengths.Order());
        Assert.Empty(fleet.Ships);
        Assert.False(fleet.TryRemoveAt(new(1, 0)));
    }

    [Fact]
    public void Reprendre_un_navire_pose_le_replace_a_un_autre_endroit_en_une_operation_atomique()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        Assert.Null(fleet.TryMoveAt(new(1, 0), new(5, 5), Orientation.Vertical));

        Assert.Equal<Coordinate>([new(5, 5), new(5, 6), new(5, 7)], Ordered(Assert.Single(fleet.Ships).Cells));
        Assert.Equal<int>([2], fleet.RemainingLengths);
    }

    [Fact]
    public void Reprendre_un_navire_pose_avec_une_nouvelle_position_invalide_est_refuse_sans_rien_changer()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);
        fleet.TryPlace(new(7, 0), 2, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.AdjacentShip, fleet.TryMoveAt(new(1, 0), new(6, 0), Orientation.Horizontal));

        var first = fleet.Ships.Single(ship => ship.Cells.Contains(new Coordinate(0, 0)));
        var second = fleet.Ships.Single(ship => ship.Cells.Contains(new Coordinate(7, 0)));

        Assert.Equal<Coordinate>([new(0, 0), new(1, 0), new(2, 0)], Ordered(first.Cells));
        Assert.Equal<Coordinate>([new(7, 0), new(8, 0)], Ordered(second.Cells));
    }

    [Fact]
    public void Les_origines_valides_excluent_les_cases_refusees_par_les_regles()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        var origins = fleet.ValidOrigins(2, Orientation.Horizontal);

        // Ni chevauchement, ni contact par un côté, ni débordement : (0,0) et (0,1) sont exclus, (0,2) reste.
        Assert.DoesNotContain(new Coordinate(0, 0), origins);
        Assert.DoesNotContain(new Coordinate(0, 1), origins);
        Assert.Contains(new Coordinate(0, 2), origins);
        Assert.DoesNotContain(new Coordinate(9, 5), origins);
        Assert.All(origins, origin => Assert.Null(
            PlacementRules.Check(PlacementRules.Cells(origin, 2, Orientation.Horizontal), 10, 10, fleet.Ships)));
    }

    [Fact]
    public void Une_longueur_deja_posee_n_a_plus_aucune_origine_valide()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        Assert.Empty(fleet.ValidOrigins(3, Orientation.Horizontal));
    }

    [Fact]
    public void Le_tirage_aleatoire_pose_toute_la_flotte()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        Assert.True(fleet.TryPlaceAtRandom(new Random(0)));

        Assert.True(fleet.IsComplete);
        Assert.Equal(TwoShips.Order(), fleet.Ships.Select(ship => ship.Cells.Count).Order());
    }

    [Fact]
    public void Demarrer_avec_une_flotte_incomplete_est_refuse()
    {
        var game = NewGame(TwoShips);
        game.TryPlaceShip(new(0, 0), 3, Orientation.Horizontal);

        Assert.Equal(StartRejection.FleetIncomplete, game.TryStart(ChooseComputerTarget, out var shots));

        Assert.Empty(shots);
        Assert.Equal(GamePhase.Setup, game.Phase);
    }

    [Fact]
    public void Un_placement_accepte_remplace_la_grille_du_joueur()
    {
        var game = NewGame(TwoShips);
        var before = game.PlayerBoard;

        Assert.Null(game.TryPlaceShip(new(0, 0), 3, Orientation.Horizontal));

        // Board reste immuable : le placement en produit une nouvelle.
        Assert.NotSame(before, game.PlayerBoard);
        Assert.Single(game.PlayerBoard.Ships);
    }

    [Fact]
    public void Un_placement_apres_le_demarrage_est_refuse_et_ne_change_pas_la_grille()
    {
        var game = NewGame([1]);
        Assert.Null(game.TryPlaceShip(new(0, 0), 1, Orientation.Horizontal));
        Assert.Null(game.TryStart(ChooseComputerTarget, out _));
        var playerBoard = game.PlayerBoard;

        Assert.Equal(PlacementRejection.NotInSetup, game.TryPlaceShip(new(5, 5), 1, Orientation.Horizontal));
        Assert.Equal(PlacementRejection.NotInSetup, game.TryRemoveShipAt(new(0, 0)));
        Assert.Equal(PlacementRejection.NotInSetup, game.TryRotateShipAt(new(0, 0)));
        Assert.Equal(PlacementRejection.NotInSetup, game.TryPlaceFleetAtRandom());
        Assert.Empty(game.ValidOrigins(1, Orientation.Horizontal));

        // La flotte a été figée au démarrage : aucune de ces opérations n'a d'objet à modifier.
        Assert.Same(playerBoard, game.PlayerBoard);
        Assert.Equal<Coordinate>([new(0, 0)], Assert.Single(game.PlayerBoard.Ships).Cells);
        Assert.Empty(game.RemainingShipLengths);
    }

    [Fact]
    public void Un_navire_pivote_autour_de_la_case_selectionnee()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(2, 2), 3, Orientation.Horizontal);

        Assert.Null(fleet.TryRotateAt(new(3, 2)));

        Assert.Equal<Coordinate>([new(3, 1), new(3, 2), new(3, 3)], Ordered(Assert.Single(fleet.Ships).Cells));
        Assert.Equal<int>([2], fleet.RemainingLengths);
    }

    [Fact]
    public void Un_navire_pivote_deux_fois_revient_a_sa_position_de_depart()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(4, 4), 3, Orientation.Horizontal);

        Assert.Null(fleet.TryRotateAt(new(4, 4)));
        Assert.Null(fleet.TryRotateAt(new(4, 4)));

        Assert.Equal<Coordinate>([new(4, 4), new(5, 4), new(6, 4)], Ordered(Assert.Single(fleet.Ships).Cells));
    }

    [Fact]
    public void Un_navire_ne_se_gene_pas_lui_meme_en_pivotant()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(5, 5), 2, Orientation.Horizontal);

        // Le navire pivoté garde sa case d'origine et jouxte celle qu'il occupait : sans exclure
        // le navire de sa propre validation, ce serait refusé en Overlap ou en AdjacentShip.
        Assert.Null(fleet.TryRotateAt(new(5, 5)));

        Assert.Equal<Coordinate>([new(5, 5), new(5, 6)], Ordered(Assert.Single(fleet.Ships).Cells));
    }

    [Fact]
    public void Un_navire_qui_sortirait_de_la_grille_en_pivotant_est_refuse_sans_rien_changer()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 8), 3, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.OutOfBounds, fleet.TryRotateAt(new(0, 8)));

        // La flotte n'a pas été démontée entre-temps : elle reste exactement ce qu'elle était.
        Assert.Equal<Coordinate>([new(0, 8), new(1, 8), new(2, 8)], Ordered(Assert.Single(fleet.Ships).Cells));
        Assert.Equal<int>([2], fleet.RemainingLengths);
    }

    [Fact]
    public void Un_navire_qui_en_toucherait_un_autre_en_pivotant_est_refuse_sans_rien_changer()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);
        fleet.TryPlace(new(1, 2), 2, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.AdjacentShip, fleet.TryRotateAt(new(0, 0)));

        Assert.Equal(2, fleet.Ships.Count);
        Assert.Contains(fleet.Ships, ship => ship.Occupies(new(2, 0)));
        Assert.Empty(fleet.RemainingLengths);
    }

    [Fact]
    public void Pivoter_une_case_vide_est_refuse()
    {
        var fleet = NewFleet();
        fleet.TryPlace(new(0, 0), 3, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.NoShipHere, fleet.TryRotateAt(new(5, 5)));

        Assert.Single(fleet.Ships);
    }

    [Fact]
    public void Une_rotation_acceptee_remplace_la_grille_du_joueur()
    {
        var game = NewGame(TwoShips);
        game.TryPlaceShip(new(0, 0), 3, Orientation.Horizontal);
        var before = game.PlayerBoard;

        Assert.Null(game.TryRotateShipAt(new(0, 0)));

        Assert.NotSame(before, game.PlayerBoard);
        Assert.Equal<Coordinate>([new(0, 0), new(0, 1), new(0, 2)], Ordered(Assert.Single(game.PlayerBoard.Ships).Cells));
    }
}
