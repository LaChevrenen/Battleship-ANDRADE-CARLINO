using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BattleShip.Models;
using BattleShip.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BattleShip.Tests.Api;

public sealed class PlacementEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Graine fixe, comme dans GameEndpointsTests : aucune de ces vérifications ne dépend de qui commence.
    private const int Seed = 20260916;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient client;

    public PlacementEndpointsTests(WebApplicationFactory<Program> factory) =>
        client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(new Random(Seed))))
            .CreateClient();

    private async Task<GameStateDto> CreateGame() =>
        (await (await client.PostAsync("/games", null)).Content.ReadFromJsonAsync<GameStateDto>(Json))!;

    private Task<HttpResponseMessage> PlaceShip(Guid id, int column, int row, int length, Orientation orientation, int version) =>
        client.PostAsJsonAsync($"/games/{id}/ships", new { column, row, length, orientation = orientation.ToString(), expectedVersion = version }, Json);

    private Task<HttpResponseMessage> RemoveShip(Guid id, int column, int row, int version) =>
        client.PostAsJsonAsync($"/games/{id}/ships/remove", new { column, row, expectedVersion = version }, Json);

    private Task<HttpResponseMessage> RotateShip(Guid id, int column, int row, int version) =>
        client.PostAsJsonAsync($"/games/{id}/ships/rotate", new { column, row, expectedVersion = version }, Json);

    private async Task<PlacementOriginsDto> ValidOrigins(Guid id, int length, Orientation orientation) =>
        (await client.GetFromJsonAsync<PlacementOriginsDto>($"/games/{id}/placements?length={length}&orientation={orientation}", Json))!;

    private Task<HttpResponseMessage> PlaceAtRandom(Guid id, int version) =>
        client.PostAsJsonAsync($"/games/{id}/fleet/random", new { expectedVersion = version }, Json);

    private async Task<GameStateDto> GetState(Guid id) =>
        (await client.GetFromJsonAsync<GameStateDto>($"/games/{id}", Json))!;

    private static async Task<GameStateDto> State(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!;

    private static async Task<string?> RejectionOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("rejection").GetString();

    [Fact]
    public async Task Une_partie_creee_annonce_toutes_les_longueurs_a_poser()
    {
        var created = await CreateGame();

        Assert.Equal<int>([2, 3, 3, 4, 5], created.Player.RemainingShipLengths.Order());
        Assert.Empty(created.Player.Ships);
    }

    [Fact]
    public async Task Poser_un_navire_le_retire_des_longueurs_restantes_et_augmente_la_version()
    {
        var created = await CreateGame();

        var response = await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await State(response);
        Assert.Equal(created.Version + 1, state.Version);
        Assert.Equal<int>([2, 3, 3, 4], state.Player.RemainingShipLengths.Order());
        Assert.Equal<Coordinate>(
            [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)],
            Assert.Single(state.Player.Ships));
    }

    [Fact]
    public async Task Un_navire_qui_sort_de_la_grille_renvoie_400_sur_la_cle_origin()
    {
        var created = await CreateGame();

        var response = await PlaceShip(created.Id, 7, 0, 5, Orientation.Horizontal, created.Version);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.True(body.GetProperty("errors").TryGetProperty("origin", out _));
    }

    [Theory]
    [InlineData(2, 0, "Overlap")]
    [InlineData(0, 1, "AdjacentShip")]
    public async Task Un_navire_mal_place_est_refuse_avec_son_motif(int column, int row, string expectedRejection)
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var response = await PlaceShip(created.Id, column, row, 4, Orientation.Horizontal, afterFirst.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(expectedRejection, await RejectionOf(response));
        // Un refus ne consomme rien : la version et les longueurs restantes n'ont pas bougé.
        var state = await client.GetFromJsonAsync<GameStateDto>($"/games/{created.Id}", Json);
        Assert.Equal(afterFirst.Version, state!.Version);
        Assert.Equal<int>([2, 3, 3, 4], state.Player.RemainingShipLengths.Order());
    }

    [Fact]
    public async Task Poser_une_longueur_deja_epuisee_est_refuse()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var response = await PlaceShip(created.Id, 0, 5, 5, Orientation.Horizontal, afterFirst.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("LengthNotAvailable", await RejectionOf(response));
    }

    [Fact]
    public async Task Poser_depuis_une_version_depassee_est_refuse_sans_rien_changer()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var response = await PlaceShip(created.Id, 0, 5, 4, Orientation.Horizontal, created.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StaleVersion", await RejectionOf(response));
        var state = await client.GetFromJsonAsync<GameStateDto>($"/games/{created.Id}", Json);
        Assert.Equal(afterFirst.Version, state!.Version);
        Assert.Single(state.Player.Ships);
    }

    [Fact]
    public async Task Retirer_le_dernier_navire_rend_sa_longueur_puis_refuse_si_la_grille_est_vide()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        // Une case du milieu du navire : le joueur clique n'importe où dessus.
        var undone = await State(await RemoveShip(created.Id, 2, 0, afterFirst.Version));

        Assert.Empty(undone.Player.Ships);
        Assert.Equal<int>([2, 3, 3, 4, 5], undone.Player.RemainingShipLengths.Order());
        var empty = await RemoveShip(created.Id, 2, 0, undone.Version);
        Assert.Equal(HttpStatusCode.Conflict, empty.StatusCode);
        Assert.Equal("NoShipHere", await RejectionOf(empty));
    }

    private Task<HttpResponseMessage> SetDifficulty(Guid id, string difficulty, int version) =>
        client.PostAsJsonAsync($"/games/{id}/difficulty", new { difficulty, expectedVersion = version }, Json);

    [Fact]
    public async Task Une_partie_neuve_annonce_le_niveau_normal()
    {
        var created = await CreateGame();

        Assert.Equal(AiDifficulty.Normal, created.Difficulty);
    }

    [Fact]
    public async Task Choisir_un_niveau_pendant_la_preparation_le_retient()
    {
        var created = await CreateGame();

        var changed = await State(await SetDifficulty(created.Id, "Hard", created.Version));

        Assert.Equal(AiDifficulty.Hard, changed.Difficulty);
        Assert.Equal(created.Version + 1, changed.Version);
        // Relu séparément : le niveau fait partie de l'état, il survit à une nouvelle lecture.
        Assert.Equal(AiDifficulty.Hard, (await GetState(created.Id)).Difficulty);
    }

    [Fact]
    public async Task Changer_de_niveau_apres_le_demarrage_est_refuse()
    {
        var created = await CreateGame();
        var placed = await State(await PlaceAtRandom(created.Id, created.Version));
        var started = await client.PostAsync($"/games/{created.Id}/start", null);
        var afterStart = await GetState(created.Id);

        var response = await SetDifficulty(created.Id, "Easy", afterStart.Version);

        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NotInSetup", await RejectionOf(response));
        Assert.Equal(AiDifficulty.Normal, (await GetState(created.Id)).Difficulty);
        Assert.NotEqual(placed.Version, afterStart.Version);
    }

    [Fact]
    public async Task Choisir_un_niveau_depuis_une_version_depassee_est_refuse()
    {
        var created = await CreateGame();
        var changed = await State(await SetDifficulty(created.Id, "Easy", created.Version));

        var response = await SetDifficulty(created.Id, "Hard", created.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StaleVersion", await RejectionOf(response));
        Assert.Equal(AiDifficulty.Easy, (await GetState(created.Id)).Difficulty);
        Assert.Equal(changed.Version, (await GetState(created.Id)).Version);
    }

    [Theory]
    [InlineData("{\"expectedVersion\": 0}")]
    [InlineData("{\"difficulty\": \"Impossible\", \"expectedVersion\": 0}")]
    [InlineData("{\"difficulty\": \"Hard\"}")]
    public async Task Une_demande_de_niveau_mal_formee_renvoie_400(string body)
    {
        var created = await CreateGame();

        var response = await client.PostAsync(
            $"/games/{created.Id}/difficulty", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Faire_pivoter_un_navire_le_redresse_autour_de_la_case_visee()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 4, 5, Orientation.Horizontal, created.Version));

        // La case visée est le pivot : elle reste occupée, et le navire tourne autour d'elle.
        var rotated = await State(await RotateShip(created.Id, 2, 4, afterFirst.Version));

        Assert.Equal<Coordinate>(
            [new(2, 2), new(2, 3), new(2, 4), new(2, 5), new(2, 6)],
            Assert.Single(rotated.Player.Ships).OrderBy(cell => cell.Row).ThenBy(cell => cell.Column));
        Assert.Equal(afterFirst.Version + 1, rotated.Version);
        Assert.Equal<int>([2, 3, 3, 4], rotated.Player.RemainingShipLengths.Order());
    }

    [Fact]
    public async Task Faire_pivoter_une_case_sans_navire_est_refuse()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var response = await RotateShip(created.Id, 9, 9, afterFirst.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NoShipHere", await RejectionOf(response));
    }

    [Fact]
    public async Task Un_navire_qui_ne_tient_pas_apres_rotation_est_refuse_sans_rien_changer()
    {
        var created = await CreateGame();
        // Posé sur l'avant-dernière ligne : à la verticale, il sortirait de la grille.
        var afterFirst = await State(await PlaceShip(created.Id, 0, 8, 5, Orientation.Horizontal, created.Version));

        var response = await RotateShip(created.Id, 0, 8, afterFirst.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("OutOfBounds", await RejectionOf(response));

        // La flotte n'a pas été démontée entre-temps : même version, même navire, mêmes cases.
        var state = await GetState(created.Id);
        Assert.Equal(afterFirst.Version, state.Version);
        Assert.Equal(5, Assert.Single(state.Player.Ships).Count);
        Assert.Equal<int>([2, 3, 3, 4], state.Player.RemainingShipLengths.Order());
    }

    [Fact]
    public async Task Pivoter_depuis_une_version_depassee_est_refuse_sans_rien_changer()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var response = await RotateShip(created.Id, 2, 0, created.Version);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StaleVersion", await RejectionOf(response));
        var state = await GetState(created.Id);
        Assert.Equal(afterFirst.Version, state.Version);
        Assert.Contains(new Coordinate(4, 0), Assert.Single(state.Player.Ships));
    }

    [Fact]
    public async Task Une_demande_de_rotation_sans_case_renvoie_400()
    {
        var created = await CreateGame();

        var response = await client.PostAsJsonAsync(
            $"/games/{created.Id}/ships/rotate", new { expectedVersion = created.Version }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Le_serveur_annonce_les_origines_valides_du_navire_choisi()
    {
        var created = await CreateGame();
        var afterFirst = await State(await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version));

        var origins = await ValidOrigins(created.Id, 4, Orientation.Horizontal);

        Assert.Equal(4, origins.Length);
        Assert.Equal(Orientation.Horizontal, origins.Orientation);
        // Collé sous le navire posé, ou débordant à droite : ces origines ne sont pas proposées.
        Assert.DoesNotContain(new Coordinate(0, 1), origins.Origins);
        Assert.DoesNotContain(new Coordinate(7, 5), origins.Origins);
        Assert.Contains(new Coordinate(0, 2), origins.Origins);
        Assert.Equal(afterFirst.Version, (await GetState(created.Id)).Version);
    }

    [Fact]
    public async Task Les_origines_valides_d_une_longueur_deja_posee_sont_vides()
    {
        var created = await CreateGame();
        await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version);

        Assert.Empty((await ValidOrigins(created.Id, 5, Orientation.Horizontal)).Origins);
    }

    [Theory]
    [InlineData("length=0&orientation=Horizontal")]
    [InlineData("orientation=Horizontal")]
    [InlineData("length=4")]
    public async Task Une_demande_d_origines_mal_formee_renvoie_400(string query)
    {
        var created = await CreateGame();

        var response = await client.GetAsync($"/games/{created.Id}/placements?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Les_origines_valides_d_une_partie_inconnue_renvoient_404()
    {
        var response = await client.GetAsync($"/games/{Guid.NewGuid()}/placements?length=4&orientation=Horizontal");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Le_tirage_aleatoire_pose_toute_la_flotte_et_permet_de_demarrer()
    {
        var created = await CreateGame();

        var placed = await State(await PlaceAtRandom(created.Id, created.Version));

        Assert.Empty(placed.Player.RemainingShipLengths);
        Assert.Equal<int>([2, 3, 3, 4, 5], placed.Player.Ships.Select(ship => ship.Count).Order());
        var started = await client.PostAsync($"/games/{created.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
    }

    [Fact]
    public async Task Demarrer_sans_flotte_complete_est_refuse()
    {
        var created = await CreateGame();
        await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, created.Version);

        var response = await client.PostAsync($"/games/{created.Id}/start", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("FleetIncomplete", await RejectionOf(response));
    }

    [Fact]
    public async Task Apres_le_demarrage_la_flotte_ne_peut_plus_changer()
    {
        var created = await CreateGame();
        var placed = await State(await PlaceAtRandom(created.Id, created.Version));
        var started = (await (await client.PostAsync($"/games/{created.Id}/start", null)).Content.ReadFromJsonAsync<TurnDto>(Json))!.State;

        var place = await PlaceShip(created.Id, 0, 0, 5, Orientation.Horizontal, started.Version);
        var undo = await RemoveShip(created.Id, 0, 0, started.Version);
        var random = await PlaceAtRandom(created.Id, started.Version);

        Assert.Equal("NotInSetup", await RejectionOf(place));
        // « Défaire » après le démarrage doit dire que la préparation est finie, pas qu'il n'y a rien à retirer.
        Assert.Equal("NotInSetup", await RejectionOf(undo));
        Assert.Equal("NotInSetup", await RejectionOf(random));
        var state = await client.GetFromJsonAsync<GameStateDto>($"/games/{created.Id}", Json);
        Assert.Equal(placed.Player.Ships.Count, state!.Player.Ships.Count);
        Assert.Equal(started.Version, state.Version);
    }
}
