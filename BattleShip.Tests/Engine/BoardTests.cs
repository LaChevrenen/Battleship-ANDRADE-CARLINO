using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class BoardTests
{
    private static Board BoardWith(params Ship[] ships) => new(10, 10, ships);

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 10)]
    public void Un_tir_hors_grille_est_refuse(int column, int row)
    {
        var board = BoardWith(new Ship([new(4, 4), new(5, 4)]));

        var result = board.ReceiveShot(new(column, row));

        Assert.Equal(ShotResult.Rejected(ShotRejection.OutOfBounds), result);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(0, 9)]
    [InlineData(9, 9)]
    public void Un_tir_sur_un_coin_de_la_grille_est_accepte(int column, int row)
    {
        var board = BoardWith(new Ship([new(4, 4), new(5, 4)]));

        var result = board.ReceiveShot(new(column, row));

        Assert.Equal(ShotResult.Miss, result);
    }

    [Fact]
    public void Un_tir_sur_une_case_sans_navire_est_a_l_eau()
    {
        var board = BoardWith(new Ship([new(4, 4), new(5, 4)]));

        Assert.Equal(ShotResult.Miss, board.ReceiveShot(new(4, 5)));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    public void Un_tir_sur_une_case_deja_tiree_est_refuse(int column, int row)
    {
        var board = BoardWith(new Ship([new(4, 4), new(5, 4)]));
        board.ReceiveShot(new(column, row));

        var result = board.ReceiveShot(new(column, row));

        Assert.Equal(ShotResult.Rejected(ShotRejection.AlreadyTargeted), result);
    }

    [Fact]
    public void Un_navire_n_est_coule_qu_a_son_dernier_segment()
    {
        var ship = new Ship([new(1, 1), new(1, 2), new(1, 3)]);
        var board = BoardWith(ship);

        Assert.Equal(ShotResult.Hit, board.ReceiveShot(new(1, 1)));
        Assert.Equal(ShotResult.Hit, board.ReceiveShot(new(1, 3)));
        Assert.Equal(ShotResult.Sunk(ship), board.ReceiveShot(new(1, 2)));
    }

    [Fact]
    public void Retirer_sur_un_segment_touche_ne_coule_pas_le_navire()
    {
        var ship = new Ship([new(1, 1), new(1, 2)]);
        var board = BoardWith(ship);
        board.ReceiveShot(new(1, 1));

        Assert.False(board.ReceiveShot(new(1, 1)).IsAccepted);
        Assert.Equal(ShotResult.Sunk(ship), board.ReceiveShot(new(1, 2)));
    }

    [Fact]
    public void Le_coule_designe_le_navire_touche_et_pas_un_autre()
    {
        var small = new Ship([new(0, 0)]);
        var large = new Ship([new(5, 5), new(6, 5)]);
        var board = BoardWith(large, small);

        var result = board.ReceiveShot(new(0, 0));

        Assert.Same(small, result.SunkShip);
    }

    [Fact]
    public void La_flotte_n_est_detruite_qu_apres_le_dernier_navire_coule()
    {
        var board = BoardWith(new Ship([new(0, 0)]), new Ship([new(5, 5)]));

        board.ReceiveShot(new(0, 0));
        Assert.False(board.AllShipsSunk);

        board.ReceiveShot(new(5, 5));
        Assert.True(board.AllShipsSunk);
    }
}
