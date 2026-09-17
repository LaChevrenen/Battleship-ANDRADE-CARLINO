using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BattleShip.Models;
using BattleShip.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BattleShip.Tests.Api;

public sealed class GameCreationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int Seed = 20260917;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient client;

    public GameCreationEndpointTests(WebApplicationFactory<Program> factory) =>
        client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(new Random(Seed))))
            .CreateClient();

    private Task<HttpResponseMessage> CreateCustom(
        int width, int height, IReadOnlyDictionary<int, int> shipCounts, bool allowAdjacentShips, bool specialAttacksEnabled = true) =>
        client.PostAsJsonAsync("/games", new { width, height, shipCounts, allowAdjacentShips, specialAttacksEnabled }, Json);

    private Task<HttpResponseMessage> PlaceShip(Guid id, int column, int row, int length, Orientation orientation, int version) =>
        client.PostAsJsonAsync($"/games/{id}/ships", new { column, row, length, orientation = orientation.ToString(), expectedVersion = version }, Json);

    private static async Task<GameStateDto> State(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!;

    private static async Task<string?> RejectionOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("rejection").GetString();

    [Fact]
    public async Task Un_corps_absent_cree_toujours_une_partie_classique()
    {
        var response = await client.PostAsync("/games", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await State(response);
        Assert.Equal<int>([2, 3, 3, 4, 5], created.Player.RemainingShipLengths.Order());
    }

    [Fact]
    public async Task Une_configuration_plausible_cree_une_partie_a_la_bonne_taille()
    {
        var response = await CreateCustom(6, 6, new Dictionary<int, int> { [3] = 1, [2] = 1 }, allowAdjacentShips: false);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await State(response);
        Assert.Equal(6, created.Player.Width);
        Assert.Equal(6, created.Player.Height);
        Assert.Equal<int>([2, 3], created.Player.RemainingShipLengths.Order());
        Assert.False(created.AllowAdjacentShips);
    }

    [Fact]
    public async Task Le_reglage_de_contact_choisi_a_la_creation_est_annonce_dans_l_etat()
    {
        var response = await CreateCustom(6, 6, new Dictionary<int, int> { [1] = 1 }, allowAdjacentShips: true);

        var created = await State(response);
        Assert.True(created.AllowAdjacentShips);
    }

    [Fact]
    public async Task Une_partie_classique_annonce_le_contact_interdit()
    {
        var created = await State(await client.PostAsync("/games", null));

        Assert.False(created.AllowAdjacentShips);
    }

    [Theory]
    [InlineData(4, 10)] // largeur sous la borne
    [InlineData(16, 10)] // largeur au-dessus de la borne
    public async Task Une_grille_hors_bornes_renvoie_400(int width, int height)
    {
        var response = await CreateCustom(width, height, new Dictionary<int, int> { [3] = 1 }, allowAdjacentShips: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)] // sous la borne des longueurs
    [InlineData(6)] // au-dessus de la borne des longueurs
    public async Task Une_longueur_de_navire_hors_bornes_renvoie_400(int length)
    {
        var response = await CreateCustom(10, 10, new Dictionary<int, int> { [length] = 1 }, allowAdjacentShips: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Plus_de_trois_navires_d_une_meme_longueur_renvoie_400()
    {
        var response = await CreateCustom(10, 10, new Dictionary<int, int> { [3] = 4 }, allowAdjacentShips: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Une_flotte_entierement_vide_renvoie_400()
    {
        var response = await CreateCustom(10, 10, new Dictionary<int, int> { [3] = 0 }, allowAdjacentShips: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Une_flotte_qui_ne_tient_pas_sur_la_grille_renvoie_409_FleetDoesNotFit()
    {
        // 5x5 = 25 cases ; 3 navires de 5 et 3 de 4 réclament 27 cases : aucune disposition ne peut
        // tenir, quel que soit le réglage de contact.
        var counts = new Dictionary<int, int> { [5] = 3, [4] = 3 };

        var response = await CreateCustom(5, 5, counts, allowAdjacentShips: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("FleetDoesNotFit", await RejectionOf(response));
    }

    [Fact]
    public async Task Le_contact_autorise_a_la_creation_est_applique_au_placement_manuel()
    {
        var created = await State(await CreateCustom(6, 6, new Dictionary<int, int> { [1] = 2 }, allowAdjacentShips: true));

        var first = await State(await PlaceShip(created.Id, 0, 0, 1, Orientation.Horizontal, created.Version));
        // (1,0) touche (0,0) par un côté : refusé par défaut, mais ce réglage l'autorise.
        var second = await PlaceShip(created.Id, 1, 0, 1, Orientation.Horizontal, first.Version);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Le_contact_reste_refuse_quand_il_n_est_pas_autorise_a_la_creation()
    {
        var created = await State(await CreateCustom(6, 6, new Dictionary<int, int> { [1] = 2 }, allowAdjacentShips: false));

        var first = await State(await PlaceShip(created.Id, 0, 0, 1, Orientation.Horizontal, created.Version));
        var second = await PlaceShip(created.Id, 1, 0, 1, Orientation.Horizontal, first.Version);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("AdjacentShip", await RejectionOf(second));
    }
}
