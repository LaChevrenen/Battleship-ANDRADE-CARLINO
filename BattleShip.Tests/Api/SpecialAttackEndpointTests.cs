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

public sealed class SpecialAttackEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int Seed = 20260918;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient client;

    public SpecialAttackEndpointTests(WebApplicationFactory<Program> factory) =>
        client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(new Random(Seed))))
            .CreateClient();

    // Partie classique en apparence, mais les attaques spéciales n'y sont plus jamais activées :
    // les mécanismes testés ici ont besoin d'un jeu personnalisé pour les activer explicitement.
    private Task<GameStateDto> CreateStartedGame() => CreateStartedCustomGame(specialAttacksEnabled: true);

    private async Task<GameStateDto> CreateStartedCustomGame(bool specialAttacksEnabled)
    {
        var body = new
        {
            width = 10,
            height = 10,
            shipCounts = new Dictionary<int, int> { [5] = 1, [4] = 1, [3] = 2, [2] = 1 },
            allowAdjacentShips = false,
            specialAttacksEnabled,
        };
        var created = (await (await client.PostAsJsonAsync("/games", body, Json)).Content.ReadFromJsonAsync<GameStateDto>(Json))!;
        var placed = await client.PostAsJsonAsync($"/games/{created.Id}/fleet/random", new { expectedVersion = created.Version }, Json);
        placed.EnsureSuccessStatusCode();
        var response = await client.PostAsync($"/games/{created.Id}/start", null);
        return (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!.State;
    }

    private Task<HttpResponseMessage> Fire(Guid id, int column, int row, int version, bool specialAttack = false) =>
        client.PostAsJsonAsync($"/games/{id}/shots", new { column, row, expectedVersion = version, specialAttack }, Json);

    private static async Task<TurnDto> Turn(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!;

    private static async Task<string?> RejectionOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("rejection").GetString();

    // Cinq cases distinctes, jamais visées par l'attaque spéciale des tests qui suivent. Touché ou
    // raté n'a pas d'importance ici : seul le nombre de tirs acceptés fait avancer la jauge.
    private async Task<GameStateDto> ChargePlayer(Guid id, GameStateDto state)
    {
        Coordinate[] cells = [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0)];
        foreach (var cell in cells)
        {
            var response = await Fire(id, cell.Column, cell.Row, state.Version);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            state = (await Turn(response)).State;
        }

        return state;
    }

    [Fact]
    public async Task Une_attaque_speciale_sans_charge_renvoie_409_SpecialAttackNotCharged()
    {
        var state = await CreateStartedGame();

        var response = await Fire(state.Id, 5, 5, state.Version, specialAttack: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SpecialAttackNotCharged", await RejectionOf(response));
    }

    [Fact]
    public async Task Une_attaque_speciale_chargee_est_acceptee_et_compte_comme_un_seul_tir()
    {
        var state = await CreateStartedGame();
        state = await ChargePlayer(state.Id, state);
        var versionBefore = state.Version;

        var response = await Fire(state.Id, 5, 5, state.Version, specialAttack: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var turn = await Turn(response);
        Assert.NotNull(turn.PlayerOutcome);
        Assert.Equal(versionBefore + 1, turn.State.Version);
        // Comptée comme un seul tir : le sixième tir accepté du joueur, pas cinq de plus.
        Assert.Equal(6, turn.State.Statistics.TotalShots);
    }

    [Fact]
    public async Task Une_attaque_speciale_chargee_hors_grille_renvoie_400_sur_la_cle_target()
    {
        var state = await CreateStartedGame();
        state = await ChargePlayer(state.Id, state);

        var response = await Fire(state.Id, 10, 0, state.Version, specialAttack: true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.True(body.GetProperty("errors").TryGetProperty("target", out _));
    }

    [Fact]
    public async Task Une_attaque_speciale_visant_une_case_deja_tiree_renvoie_409_AlreadyTargeted()
    {
        var state = await CreateStartedGame();
        state = await ChargePlayer(state.Id, state);

        // (0, 0) fait partie des cases tirées pendant la charge.
        var response = await Fire(state.Id, 0, 0, state.Version, specialAttack: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("AlreadyTargeted", await RejectionOf(response));
    }

    [Fact]
    public async Task Une_attaque_speciale_depuis_une_version_depassee_renvoie_409_StaleVersion()
    {
        var state = await CreateStartedGame();
        var chargedState = await ChargePlayer(state.Id, state);

        // Version d'avant la charge, volontairement dépassée.
        var response = await Fire(chargedState.Id, 5, 5, state.Version, specialAttack: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StaleVersion", await RejectionOf(response));
    }

    [Fact]
    public async Task Une_partie_classique_n_a_jamais_les_attaques_speciales_activees()
    {
        var created = (await (await client.PostAsync("/games", null)).Content.ReadFromJsonAsync<GameStateDto>(Json))!;

        Assert.False(created.SpecialAttacksEnabled);
    }

    [Fact]
    public async Task Une_partie_personnalisee_annonce_le_reglage_choisi_pour_les_attaques_speciales()
    {
        var state = await CreateStartedCustomGame(specialAttacksEnabled: false);

        Assert.False(state.SpecialAttacksEnabled);
    }

    [Fact]
    public async Task Attaques_speciales_desactivees_la_jauge_ne_charge_jamais()
    {
        var state = await CreateStartedCustomGame(specialAttacksEnabled: false);

        // Six tirs acceptés, au-delà de l'intervalle de charge habituel (5) : si la jauge avançait
        // malgré le réglage, elle serait pleine ici.
        Coordinate[] cells = [new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0), new(5, 0)];
        foreach (var cell in cells)
        {
            var response = await Fire(state.Id, cell.Column, cell.Row, state.Version);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            state = (await Turn(response)).State;
        }

        Assert.Equal(0, state.PlayerSpecialAttackProgress);

        var special = await Fire(state.Id, 6, 0, state.Version, specialAttack: true);
        Assert.Equal(HttpStatusCode.Conflict, special.StatusCode);
        Assert.Equal("SpecialAttackNotCharged", await RejectionOf(special));
    }

    [Fact]
    public async Task Un_tir_sans_le_champ_specialAttack_reste_un_tir_normal()
    {
        // Absence du champ dans le JSON brut, pas seulement false explicite : c'est ce que tout
        // client déjà existant envoie encore.
        var state = await CreateStartedGame();

        var response = await client.PostAsJsonAsync(
            $"/games/{state.Id}/shots", new { column = 0, row = 0, expectedVersion = state.Version }, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var turn = await Turn(response);
        Assert.Equal(1, turn.State.Statistics.TotalShots);
    }
}
