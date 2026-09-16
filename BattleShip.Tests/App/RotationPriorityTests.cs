extern alias app;

using app::BattleShip.App.Services;
using BattleShip.Models;

namespace BattleShip.Tests.App;

public sealed class RotationPriorityTests
{
    [Fact]
    public void Une_selection_du_port_prend_priorite_sur_un_navire_pose()
    {
        var decision = ShipRotation.Resolve(selectedLength: 3, hoveredCell: new Coordinate(2, 2), ownShipCells: [new Coordinate(2, 2)]);

        Assert.True(decision.RotatePortSelection);
        Assert.Null(decision.RotatePlacedShip);
    }

    [Fact]
    public void Une_case_avec_navire_pose_est_traitee_comme_rotation_de_grille_quand_aucune_selection_du_port()
    {
        var decision = ShipRotation.Resolve(selectedLength: null, hoveredCell: new Coordinate(4, 5), ownShipCells: [new Coordinate(4, 5)]);

        Assert.False(decision.RotatePortSelection);
        Assert.Equal(new Coordinate(4, 5), decision.RotatePlacedShip);
    }

    [Fact]
    public void Aucune_rotation_n_est_demandee_si_ni_selection_ni_navire_pose()
    {
        var decision = ShipRotation.Resolve(selectedLength: null, hoveredCell: new Coordinate(7, 7), ownShipCells: [new Coordinate(2, 2)]);

        Assert.False(decision.RotatePortSelection);
        Assert.Null(decision.RotatePlacedShip);
    }
}
