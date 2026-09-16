using BattleShip.API.Validation;
using BattleShip.Protocol;

namespace BattleShip.Tests.Api;

public sealed class GetGameRequestValidatorTests
{
    private readonly GetGameRequestValidator validator = new();

    [Fact]
    public async Task Un_identifiant_de_partie_bien_forme_est_valide()
    {
        var result = await validator.ValidateAsync(new GetGameRequest { GameId = Guid.NewGuid().ToString() });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "L'identifiant de la partie est obligatoire.")]
    [InlineData("pas-un-guid", "L'identifiant de la partie n'est pas valide.")]
    public async Task Un_identifiant_vide_ou_mal_forme_produit_une_seule_erreur_explicite(string gameId, string expectedMessage)
    {
        var result = await validator.ValidateAsync(new GetGameRequest { GameId = gameId });

        var error = Assert.Single(result.Errors);
        Assert.Equal(expectedMessage, error.ErrorMessage);
    }
}
