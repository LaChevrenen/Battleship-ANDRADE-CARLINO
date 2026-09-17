using BattleShip.API.Engine;
using BattleShip.Models;

namespace BattleShip.Tests.Engine;

public sealed class GameCustomCreationTests
{
    [Fact]
    public void Une_configuration_plausible_produit_une_partie_a_la_bonne_taille()
    {
        var game = Game.TryCreate(width: 6, height: 6, shipLengths: [3, 2], allowAdjacentShips: false, new Random(0));

        Assert.NotNull(game);
        var revealed = game.PlayerBoard.Reveal();
        Assert.Equal(6, revealed.Width);
        Assert.Equal(6, revealed.Height);
        Assert.Equal<int>([2, 3], game.RemainingShipLengths.Order());
    }

    [Fact]
    public void Une_configuration_que_le_serveur_ne_peut_pas_placer_ne_cree_aucune_partie()
    {
        // 3x3 pour un navire de 5 : aucune position légale, quel que soit le nombre d'essais.
        var game = Game.TryCreate(width: 3, height: 3, shipLengths: [5], allowAdjacentShips: false, new Random(0));

        Assert.Null(game);
    }

    [Fact]
    public void Une_configuration_impossible_sans_contact_devient_possible_en_l_autorisant()
    {
        // Même grille et même flotte que le test « contact interdit » de RandomFleetPlacerTests :
        // seul le réglage de contact rend la partie créable.
        var refusee = Game.TryCreate(width: 2, height: 1, shipLengths: [1, 1], allowAdjacentShips: false, new Random(0));
        var acceptee = Game.TryCreate(width: 2, height: 1, shipLengths: [1, 1], allowAdjacentShips: true, new Random(0));

        Assert.Null(refusee);
        Assert.NotNull(acceptee);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Le_reglage_de_contact_choisi_a_la_creation_reste_lisible_toute_la_partie(bool allowAdjacentShips)
    {
        var game = Game.TryCreate(width: 6, height: 6, shipLengths: [2], allowAdjacentShips, new Random(0));

        Assert.NotNull(game);
        Assert.Equal(allowAdjacentShips, game.AllowAdjacentShips);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Le_reglage_des_attaques_speciales_choisi_a_la_creation_reste_lisible_toute_la_partie(bool specialAttacksEnabled)
    {
        var game = Game.TryCreate(width: 6, height: 6, shipLengths: [2], allowAdjacentShips: false, new Random(0),
            specialAttacksEnabled: specialAttacksEnabled);

        Assert.NotNull(game);
        Assert.Equal(specialAttacksEnabled, game.SpecialAttacksEnabled);
    }

    [Fact]
    public void Une_partie_classique_n_a_jamais_les_attaques_speciales_activees()
    {
        var game = Game.CreateWithRandomComputerFleet(new Random(0));

        Assert.False(game.SpecialAttacksEnabled);
    }

    [Fact]
    public void La_flotte_du_joueur_reprend_la_meme_configuration_que_celle_de_l_ordinateur()
    {
        var game = Game.TryCreate(width: 7, height: 8, shipLengths: [4, 3], allowAdjacentShips: true, new Random(0));

        Assert.NotNull(game);
        // Le joueur pose sa propre flotte : seules les dimensions et les longueurs sont partagées.
        Assert.Equal<int>([3, 4], game.RemainingShipLengths.Order());
        var revealed = game.PlayerBoard.Reveal();
        Assert.Equal(7, revealed.Width);
        Assert.Equal(8, revealed.Height);
    }
}
