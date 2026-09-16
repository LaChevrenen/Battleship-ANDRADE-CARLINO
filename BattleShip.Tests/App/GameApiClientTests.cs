// Voir ShipSelectionTests : l'API et l'App génèrent toutes deux les types de battleship.proto.
extern alias app;

using System.Net;
using app::BattleShip.App.Services;
using BattleShip.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BattleShip.Tests.App;

// Le client ne doit jamais laisser une réponse d'erreur remonter en exception : elle traverserait
// le gestionnaire d'événement et casserait le rendu du composant.
public sealed class GameApiClientTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly GameApiClient client;

    public GameApiClientTests(WebApplicationFactory<Program> factory) => client = new(factory.CreateClient());

    [Fact]
    public async Task Une_demande_d_origines_refusee_est_lue_sans_lever()
    {
        var created = await client.CreateAsync();

        // Longueur 0 : ce que la page envoyait quand la flotte devenait complète.
        var result = await client.ValidOriginsAsync(created.Game!.Id, 0, Orientation.Horizontal);

        Assert.Empty(result.Origins);
        Assert.NotNull(result.Refusal);
    }

    [Fact]
    public async Task Une_creation_sans_corps_d_erreur_est_lue_sans_lever()
    {
        var sansCorps = new GameApiClient(new HttpClient(new ReponseFixe(HttpStatusCode.ServiceUnavailable))
        {
            BaseAddress = new Uri("http://localhost"),
        });

        var result = await sansCorps.CreateAsync();

        Assert.Null(result.Game);
        Assert.Equal("ServiceUnavailable", result.Refusal);
    }

    // Une panne serveur ne renvoie pas toujours un ProblemDetails : ici, pas de corps du tout.
    private sealed class ReponseFixe(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status));
    }
}
