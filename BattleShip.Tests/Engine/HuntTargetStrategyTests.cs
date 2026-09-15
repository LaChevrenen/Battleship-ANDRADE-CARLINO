using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class HuntTargetStrategyTests
{
    private static RevealedBoard View(
        int width = 10,
        int height = 10,
        Coordinate[]? misses = null,
        Coordinate[]? hits = null,
        Coordinate[][]? sunkShips = null) =>
        new(width, height, (misses ?? []).ToHashSet(), (hits ?? []).ToHashSet(),
            [.. (sunkShips ?? []).Select(cells => (IReadOnlySet<Coordinate>)cells.ToHashSet())]);

    // Rejoue le choix sur 100 graines pour observer toutes les cases que la stratégie peut viser.
    private static HashSet<Coordinate> ChosenOverSeeds(RevealedBoard view) =>
        Enumerable.Range(0, 100).Select(seed => HuntTargetStrategy.ChooseTarget(view, new Random(seed))).ToHashSet();

    [Fact]
    public void En_chasse_seule_la_derniere_case_libre_peut_etre_visee()
    {
        var view = View(3, 3, misses: [new(0, 0), new(1, 0), new(2, 0), new(0, 1), new(2, 1), new(0, 2), new(1, 2), new(2, 2)]);

        Assert.Equivalent(new[] { new Coordinate(1, 1) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void En_chasse_toutes_les_cases_libres_peuvent_etre_visees()
    {
        var view = View(2, 2);

        Assert.Equivalent(new Coordinate[] { new(0, 0), new(1, 0), new(0, 1), new(1, 1) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void Une_touche_isolee_mene_a_ses_quatre_voisines_et_a_elles_seules()
    {
        var view = View(hits: [new(5, 5)]);

        Assert.Equivalent(new Coordinate[] { new(4, 5), new(6, 5), new(5, 4), new(5, 6) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void Les_voisines_deja_tirees_d_une_touche_isolee_sont_ecartees()
    {
        var view = View(misses: [new(4, 5), new(6, 5), new(5, 4)], hits: [new(5, 5)]);

        Assert.Equivalent(new[] { new Coordinate(5, 6) }, ChosenOverSeeds(view), strict: true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deux_touches_alignees_ne_menent_qu_aux_prolongements_de_la_ligne(bool horizontal)
    {
        Coordinate At(int along, int across) => horizontal ? new(along, across) : new(across, along);
        var view = View(hits: [At(4, 5), At(5, 5)]);

        Assert.Equivalent(new[] { At(3, 5), At(6, 5) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void Une_ligne_contre_le_bord_ne_se_prolonge_que_vers_l_interieur()
    {
        var view = View(hits: [new(0, 5), new(1, 5)]);

        Assert.Equivalent(new[] { new Coordinate(2, 5) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void Les_touches_d_un_navire_coule_ne_sont_plus_visees()
    {
        // Le seul prolongement du navire coulé est déjà tiré : seule la chasse peut proposer une case.
        var view = View(5, 1, misses: [new(2, 0)], hits: [new(0, 0), new(1, 0)], sunkShips: [[new(0, 0), new(1, 0)]]);

        Assert.Equivalent(new Coordinate[] { new(3, 0), new(4, 0) }, ChosenOverSeeds(view), strict: true);
    }

    [Fact]
    public void Plusieurs_navires_touches_sont_tous_cibles()
    {
        var view = View(hits: [new(4, 5), new(5, 5), new(8, 1)]);

        Assert.Equivalent(
            new Coordinate[] { new(3, 5), new(6, 5), new(7, 1), new(9, 1), new(8, 0), new(8, 2) },
            ChosenOverSeeds(view),
            strict: true);
    }
}
