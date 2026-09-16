using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class PlacementRulesTests
{
    private static readonly Ship[] Placed = [new Ship([new(4, 4), new(5, 4), new(6, 4)])];

    [Fact]
    public void Un_navire_horizontal_occupe_des_colonnes_consecutives()
    {
        var cells = PlacementRules.Cells(new(2, 7), 3, Orientation.Horizontal);

        Assert.Equal<Coordinate>([new(2, 7), new(3, 7), new(4, 7)], cells);
    }

    [Fact]
    public void Un_navire_vertical_occupe_des_lignes_consecutives()
    {
        var cells = PlacementRules.Cells(new(2, 7), 3, Orientation.Vertical);

        Assert.Equal<Coordinate>([new(2, 7), new(2, 8), new(2, 9)], cells);
    }

    [Theory]
    [InlineData(8, 0, Orientation.Horizontal)]
    [InlineData(0, 8, Orientation.Vertical)]
    [InlineData(-1, 0, Orientation.Horizontal)]
    public void Un_navire_qui_deborde_est_refuse(int column, int row, Orientation orientation)
    {
        var cells = PlacementRules.Cells(new(column, row), 3, orientation);

        Assert.Equal(PlacementRejection.OutOfBounds, PlacementRules.Check(cells, 10, 10, Placed));
    }

    [Fact]
    public void Un_navire_qui_chevauche_un_autre_est_refuse()
    {
        var cells = PlacementRules.Cells(new(5, 2), 3, Orientation.Vertical);

        Assert.Equal(PlacementRejection.Overlap, PlacementRules.Check(cells, 10, 10, Placed));
    }

    [Theory]
    [InlineData(4, 3)] // au-dessus
    [InlineData(3, 4)] // à gauche
    [InlineData(4, 5)] // en dessous
    public void Un_navire_qui_touche_un_autre_par_un_cote_est_refuse(int column, int row)
    {
        var cells = PlacementRules.Cells(new(column, row), 1, Orientation.Horizontal);

        Assert.Equal(PlacementRejection.AdjacentShip, PlacementRules.Check(cells, 10, 10, Placed));
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(7, 5)]
    public void Un_navire_qui_touche_un_autre_en_diagonale_est_accepte(int column, int row)
    {
        var cells = PlacementRules.Cells(new(column, row), 1, Orientation.Horizontal);

        Assert.Null(PlacementRules.Check(cells, 10, 10, Placed));
    }
}
