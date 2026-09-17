using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class RandomFleetPlacerTests
{
    public static IEnumerable<object[]> Seeds => Enumerable.Range(0, 50).Select(seed => new object[] { seed });

    private static Board PlaceDefaultFleet(int seed)
    {
        var result = RandomFleetPlacer.Place(
            GameRules.GridSize, GameRules.GridSize, GameRules.DefaultShipLengths, new Random(seed));

        Assert.NotNull(result.Ships);
        return new Board(GameRules.GridSize, GameRules.GridSize, result.Ships);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void La_flotte_placee_est_complete(int seed)
    {
        var board = PlaceDefaultFleet(seed);

        var lengths = board.Ships.Select(ship => ship.Cells.Count).Order();

        Assert.Equal(GameRules.DefaultShipLengths.Order(), lengths);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Chaque_navire_est_rectiligne_et_contigu(int seed)
    {
        var board = PlaceDefaultFleet(seed);

        foreach (var ship in board.Ships)
        {
            var columns = ship.Cells.Select(cell => cell.Column).Distinct().Order().ToArray();
            var rows = ship.Cells.Select(cell => cell.Row).Distinct().Order().ToArray();

            var horizontal = rows.Length == 1 && IsConsecutive(columns) && columns.Length == ship.Cells.Count;
            var vertical = columns.Length == 1 && IsConsecutive(rows) && rows.Length == ship.Cells.Count;
            Assert.True(horizontal || vertical);
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Aucun_navire_ne_deborde_de_la_grille(int seed)
    {
        var board = PlaceDefaultFleet(seed);

        Assert.All(board.Ships.SelectMany(ship => ship.Cells), cell =>
        {
            Assert.InRange(cell.Column, 0, GameRules.GridSize - 1);
            Assert.InRange(cell.Row, 0, GameRules.GridSize - 1);
        });
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Aucun_navire_ne_chevauche_un_autre(int seed)
    {
        var board = PlaceDefaultFleet(seed);

        var cells = board.Ships.SelectMany(ship => ship.Cells).ToArray();

        Assert.Equal(cells.Length, cells.Distinct().Count());
    }

    [Fact]
    public void Deux_navires_d_une_case_ne_peuvent_pas_occuper_la_meme_case()
    {
        // Un navire d'une case n'a aucune voisine interne : seul le blocage de sa propre case empêche le chevauchement.
        var result = RandomFleetPlacer.Place(1, 1, [1, 1], new Random(0));

        Assert.Null(result.Ships);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Aucun_navire_ne_touche_un_autre_par_un_cote(int seed)
    {
        var board = PlaceDefaultFleet(seed);

        foreach (var ship in board.Ships)
        {
            var otherCells = board.Ships.Where(other => other != ship).SelectMany(other => other.Cells);
            Assert.DoesNotContain(otherCells, other => ship.Cells.Any(cell => AreSideBySide(cell, other)));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Le_contact_en_diagonale_est_autorise(int seed)
    {
        // Sur 2x2, deux navires d'une case ne peuvent se placer qu'en diagonale l'un de l'autre.
        var result = RandomFleetPlacer.Place(2, 2, [1, 1], new Random(seed));

        Assert.NotNull(result.Ships);
    }

    [Fact]
    public void Le_placement_echoue_apres_1000_essais_sur_une_configuration_impossible()
    {
        var result = RandomFleetPlacer.Place(3, 3, [5], new Random(0));

        Assert.Null(result.Ships);
        Assert.Equal(1000, result.AttemptsUsed);
    }

    [Fact]
    public void Le_placement_echoue_apres_1000_essais_meme_si_le_premier_navire_tient()
    {
        // Sur 5x1, le premier navire de 3 se place toujours mais bloque la place du second.
        var result = RandomFleetPlacer.Place(5, 1, [3, 3], new Random(0));

        Assert.Null(result.Ships);
        Assert.Equal(1000, result.AttemptsUsed);
    }

    [Fact]
    public void Un_placement_reussi_au_premier_essai_compte_un_essai()
    {
        // Sur 1x1, un navire d'une case n'a qu'une position : le premier essai réussit toujours.
        var result = RandomFleetPlacer.Place(1, 1, [1], new Random(0));

        Assert.NotNull(result.Ships);
        Assert.Equal(1, result.AttemptsUsed);
    }

    [Fact]
    public void Sur_une_grille_de_deux_cases_le_contact_interdit_empeche_deux_navires_d_une_case()
    {
        // Sur 2x1, les deux seules cases sont voisines par un côté : la deuxième n'a plus de place
        // légale une fois la première occupée, tant que le contact reste interdit.
        var result = RandomFleetPlacer.Place(2, 1, [1, 1], new Random(0));

        Assert.Null(result.Ships);
    }

    [Fact]
    public void Autoriser_le_contact_rend_placable_une_configuration_sinon_impossible()
    {
        // Exactement la même grille et la même flotte que le test précédent : seul le réglage change.
        var result = RandomFleetPlacer.Place(2, 1, [1, 1], new Random(0), allowAdjacentShips: true);

        Assert.NotNull(result.Ships);
        Assert.Equal<Coordinate>([new(0, 0), new(1, 0)], result.Ships.SelectMany(ship => ship.Cells).OrderBy(cell => cell.Column));
    }

    private static bool IsConsecutive(int[] sortedValues) =>
        sortedValues.Zip(sortedValues.Skip(1)).All(pair => pair.Second == pair.First + 1);

    private static bool AreSideBySide(Coordinate a, Coordinate b) =>
        Math.Abs(a.Column - b.Column) + Math.Abs(a.Row - b.Row) == 1;
}
