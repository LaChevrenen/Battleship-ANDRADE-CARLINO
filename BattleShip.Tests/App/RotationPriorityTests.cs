extern alias app;

using app::BattleShip.App.Services;
using BattleShip.Models;

namespace BattleShip.Tests.App;

public sealed class RotationPriorityTests
{
    [Fact]
    public void Le_navire_pose_selectionne_prend_priorite_sur_la_selection_du_port()
    {
        var decision = ShipRotation.Resolve(selectedLength: 3, selectedPlacedShip: [new Coordinate(2, 2)]);

        Assert.False(decision.RotatePortSelection);
        Assert.Equal(new Coordinate(2, 2), decision.RotatePlacedShip);
    }

    [Fact]
    public void Le_centre_du_navire_pose_est_utilise_pour_la_rotation()
    {
        var decision = ShipRotation.Resolve(selectedLength: null, selectedPlacedShip: [new Coordinate(4, 5), new Coordinate(5, 5), new Coordinate(6, 5)]);

        Assert.False(decision.RotatePortSelection);
        Assert.Equal(new Coordinate(5, 5), decision.RotatePlacedShip);
    }

    [Fact]
    public void Aucune_rotation_n_est_demandee_si_ni_selection_ni_navire_pose()
    {
        var decision = ShipRotation.Resolve(selectedLength: null, selectedPlacedShip: null);

        Assert.False(decision.RotatePortSelection);
        Assert.Null(decision.RotatePlacedShip);
    }
}
