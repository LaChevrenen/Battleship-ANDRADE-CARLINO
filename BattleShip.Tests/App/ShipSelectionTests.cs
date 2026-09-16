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
    public void Une_longueur_epuisee_ne_cree_pas_une_nouvelle_selection()
    {
        Assert.Null(ShipSelection.Keep(current: 5, remaining: [4, 3, 3]));
    }

    [Fact]
    public void Sans_choix_prealable_aucune_longueur_n_est_choisie()
    {
        Assert.Null(ShipSelection.Keep(current: null, remaining: [5, 4, 3, 3, 2]));
    }
}
