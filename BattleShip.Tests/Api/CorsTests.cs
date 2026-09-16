using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BattleShip.Tests.Api;

// Ces tests vérifient les en-têtes CORS produits par l'API. Ils ne remplacent pas un essai dans un navigateur :
// aucun client de test n'applique la politique de même origine.
public sealed class CorsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string FrontOrigin = "http://localhost:5274";

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Une_reponse_au_front_expose_les_en_tetes_de_statut_grpc()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/games/{Guid.NewGuid()}");
        request.Headers.Add("Origin", FrontOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(FrontOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var exposed = Assert.Single(response.Headers.GetValues("Access-Control-Expose-Headers"));
        Assert.Contains("Grpc-Status", exposed);
        Assert.Contains("Grpc-Message", exposed);
    }

    [Fact]
    public async Task La_requete_de_pre_verification_du_service_grpc_est_acceptee()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/battleship.GameService/GetGame");
        request.Headers.Add("Origin", FrontOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-grpc-web");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(FrontOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Une_origine_inconnue_n_est_pas_autorisee()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/games/{Guid.NewGuid()}");
        request.Headers.Add("Origin", "https://site-inconnu.example");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
