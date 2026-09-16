using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class RevealedBoardTests
{
    public static IEnumerable<object[]> Seeds => Enumerable.Range(0, 20).Select(seed => new object[] { seed });

    private static IEnumerable<Coordinate> AllRevealedCells(RevealedBoard view) =>
        view.Misses.Concat(view.Hits).Concat(view.SunkShips.SelectMany(cells => cells));

    [Fact]
    public void Une_grille_neuve_ne_revele_aucune_case()
    {
        var board = new Board(10, 10, [new Ship([new(2, 2), new(3, 2)])]);

        Assert.Empty(AllRevealedCells(board.Reveal()));
    }

    [Fact]
    public void La_vue_porte_les_dimensions_de_la_grille()
    {
        var view = new Board(7, 5, [new Ship([new(2, 2)])]).Reveal();

        Assert.Equal(7, view.Width);
        Assert.Equal(5, view.Height);
    }

    [Fact]
    public void Un_tir_a_l_eau_est_revele_comme_rate_et_rien_d_autre()
    {
        var board = new Board(10, 10, [new Ship([new(2, 2), new(3, 2)])]);
        board.ReceiveShot(new(7, 7));

        var view = board.Reveal();

        Assert.Equivalent(new[] { new Coordinate(7, 7) }, view.Misses, strict: true);
        Assert.Empty(view.Hits);
        Assert.Empty(view.SunkShips);
    }

    [Fact]
    public void Un_tir_refuse_n_apparait_pas_dans_la_vue()
    {
        var board = new Board(10, 10, [new Ship([new(2, 2), new(3, 2)])]);
        board.ReceiveShot(new(10, 0));
        board.ReceiveShot(new(0, -1));

        Assert.Empty(AllRevealedCells(board.Reveal()));
    }

    [Fact]
    public void Un_navire_touche_une_fois_ne_revele_aucune_autre_de_ses_cases()
    {
        var board = new Board(10, 10, [new Ship([new(2, 2), new(3, 2), new(4, 2)])]);
        board.ReceiveShot(new(3, 2));

        var view = board.Reveal();

        Assert.Equivalent(new[] { new Coordinate(3, 2) }, view.Hits, strict: true);
        Assert.Empty(view.SunkShips);
        Assert.DoesNotContain(new Coordinate(2, 2), AllRevealedCells(view));
        Assert.DoesNotContain(new Coordinate(4, 2), AllRevealedCells(view));
    }

    [Theory]
    [InlineData(2, 0)] // voisin par un côté : interdit au placement, mais la vue ne doit pas compter dessus
    [InlineData(2, 1)] // voisin en diagonale : cas réel d'une partie
    public void Un_navire_coule_ne_revele_aucune_case_du_navire_voisin_non_coule(int neighbourColumn, int neighbourRow)
    {
        var adjacentCell = new Coordinate(neighbourColumn, neighbourRow);
        var farCell = new Coordinate(neighbourColumn + 1, neighbourRow);
        var board = new Board(10, 10, [new Ship([new(0, 0), new(1, 0)]), new Ship([adjacentCell, farCell])]);
        board.ReceiveShot(farCell);
        board.ReceiveShot(new(0, 0));
        board.ReceiveShot(new(1, 0));

        var view = board.Reveal();

        var sunkCells = Assert.Single(view.SunkShips);
        Assert.Equivalent(new[] { new Coordinate(0, 0), new Coordinate(1, 0) }, sunkCells, strict: true);
        Assert.DoesNotContain(adjacentCell, AllRevealedCells(view));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Les_cases_de_navire_revelees_sont_exactement_les_cases_touchees_et_celles_des_navires_coules(int seed)
    {
        var random = new Random(seed);
        var board = new Board(10, 10, RandomFleetPlacer.Place(10, 10, GameRules.DefaultShipLengths, random).Ships!);
        var shots = Enumerable.Range(0, 60).Select(_ => new Coordinate(random.Next(10), random.Next(10))).Distinct().ToList();
        foreach (var shot in shots)
            board.ReceiveShot(shot);

        var view = board.Reveal();

        var shipCells = board.Ships.SelectMany(ship => ship.Cells).ToHashSet();
        var expectedSunkShips = board.Ships.Where(ship => ship.Cells.All(shots.Contains)).ToList();
        Assert.Equivalent(shots.Where(shipCells.Contains), view.Hits, strict: true);
        Assert.Equivalent(shots.Where(shot => !shipCells.Contains(shot)), view.Misses, strict: true);
        Assert.Equal(expectedSunkShips.Count, view.SunkShips.Count);
        Assert.Equivalent(expectedSunkShips.SelectMany(ship => ship.Cells), view.SunkShips.SelectMany(cells => cells), strict: true);
    }

    [Fact]
    public void Une_vue_deja_calculee_ne_change_pas_apres_un_tir_suivant()
    {
        var board = new Board(10, 10, [new Ship([new(2, 2)])]);
        var before = board.Reveal();

        board.ReceiveShot(new(2, 2));

        Assert.Empty(before.Hits);
        Assert.Empty(before.SunkShips);
    }
}
