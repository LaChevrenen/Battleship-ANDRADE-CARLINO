// L'API et l'App génèrent toutes deux les types de battleship.proto : sans cet alias, le seul fait
// de référencer l'App rendrait BattleShip.Protocol.GameService ambigu dans tout le projet de tests.
extern alias app;

using app::BattleShip.App.Services;

namespace BattleShip.Tests.App;

public sealed class ShipSelectionTests
{
    [Fact]
    public void Une_flotte_complete_ne_laisse_aucune_longueur_choisie()
    {
        Assert.Null(ShipSelection.Keep(current: 5, remaining: []));
    }

    [Fact]
    public void Une_longueur_encore_disponible_reste_choisie()
    {
        Assert.Equal(4, ShipSelection.Keep(current: 4, remaining: [2, 3, 3, 4]));
    }

    [Fact]
    public void Une_longueur_epuisee_laisse_la_place_a_la_plus_petite_restante()
    {
        Assert.Equal(3, ShipSelection.Keep(current: 5, remaining: [4, 3, 3]));
    }

    [Fact]
    public void Sans_choix_prealable_la_plus_petite_longueur_est_choisie()
    {
        Assert.Equal(2, ShipSelection.Keep(current: null, remaining: [5, 4, 3, 3, 2]));
    }
}
