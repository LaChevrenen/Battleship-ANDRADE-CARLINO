using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BattleShip.Models;
using BattleShip.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BattleShip.Tests.Api;

public sealed class GameEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient client = factory.CreateClient();

    private async Task<GameStateDto> CreateGame()
    {
        var response = await client.PostAsync("/games", null);
        return (await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!;
    }

    private async Task<GameStateDto> CreateStartedGame()
    {
        var created = await CreateGame();
        var response = await client.PostAsync($"/games/{created.Id}/start", null);
        return (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!.State;
    }

    private Task<HttpResponseMessage> Fire(Guid id, object body) => client.PostAsJsonAsync($"/games/{id}/shots", body);

    private async Task<GameStateDto> GetState(Guid id) => (await client.GetFromJsonAsync<GameStateDto>($"/games/{id}", Json))!;

    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Creer_une_partie_renvoie_201_avec_son_adresse_et_un_etat_en_preparation()
    {
        var response = await client.PostAsync("/games", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var state = (await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!;
        Assert.Equal($"/games/{state.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(GamePhase.Setup, state.Phase);
        Assert.Equal(0, state.Version);
        Assert.Equal<int>([2, 3, 3, 4, 5], state.Player.Ships.Select(ship => ship.Count).Order());
    }

    [Fact]
    public async Task Les_enums_circulent_en_chaines()
    {
        var state = await CreateStartedGame();

        var raw = await client.GetStringAsync($"/games/{state.Id}");

        Assert.Contains("\"phase\":\"InProgress\"", raw);
        Assert.Contains("\"currentTurn\":\"Player\"", raw);
    }

    [Fact]
    public async Task Une_partie_inconnue_renvoie_404_en_lecture_au_demarrage_et_au_tir()
    {
        var unknown = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/games/{unknown}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/games/{unknown}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Fire(unknown, new { column = 0, row = 0, expectedVersion = 0 })).StatusCode);
    }

    [Fact]
    public async Task Demarrer_renvoie_200_puis_409_AlreadyStarted_au_second_demarrage()
    {
        var created = await CreateGame();

        var first = await client.PostAsync($"/games/{created.Id}/start", null);
        var second = await client.PostAsync($"/games/{created.Id}/start", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var state = (await first.Content.ReadFromJsonAsync<TurnDto>(Json))!.State;
        Assert.Equal(GamePhase.InProgress, state.Phase);
        Assert.Equal(Side.Player, state.CurrentTurn);
        Assert.Equal(1, state.Version);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("AlreadyStarted", (await Body(second)).GetProperty("rejection").GetString());
    }

    [Fact]
    public async Task Un_tir_sans_colonne_renvoie_400_sur_ce_champ()
    {
        var state = await CreateStartedGame();

        var response = await Fire(state.Id, new { row = 4, expectedVersion = state.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Body(response)).GetProperty("errors").TryGetProperty("Column", out _));
    }

    [Fact]
    public async Task Un_tir_avant_le_demarrage_renvoie_409_NotStarted()
    {
        var created = await CreateGame();

        var response = await Fire(created.Id, new { column = 0, row = 0, expectedVersion = created.Version });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("NotStarted", (await Body(response)).GetProperty("rejection").GetString());
    }

    [Fact]
    public async Task Un_tir_depuis_une_version_depassee_renvoie_409_StaleVersion_sans_rien_changer()
    {
        var state = await CreateStartedGame();

        var response = await Fire(state.Id, new { column = 0, row = 0, expectedVersion = state.Version - 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("StaleVersion", (await Body(response)).GetProperty("rejection").GetString());
        var after = await GetState(state.Id);
        Assert.Equal(state.Version, after.Version);
        Assert.Empty(after.Opponent.Misses);
        Assert.Empty(after.Opponent.Hits);
    }

    [Fact]
    public async Task Un_tir_hors_grille_renvoie_400_sur_la_cle_target_sans_changer_la_version()
    {
        var state = await CreateStartedGame();

        var response = await Fire(state.Id, new { column = 10, row = 0, expectedVersion = state.Version });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Body(response)).GetProperty("errors").TryGetProperty("target", out _));
        Assert.Equal(state.Version, (await GetState(state.Id)).Version);
    }

    [Fact]
    public async Task Un_tir_accepte_renvoie_200_et_augmente_la_version()
    {
        var state = await CreateStartedGame();

        var response = await Fire(state.Id, new { column = 0, row = 0, expectedVersion = state.Version });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var turn = (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!;
        Assert.NotNull(turn.PlayerOutcome);
        Assert.Equal(state.Version + 1, turn.State.Version);
    }

    [Fact]
    public async Task Retirer_sur_la_meme_case_renvoie_409_AlreadyTargeted()
    {
        var state = await CreateStartedGame();
        var first = await Fire(state.Id, new { column = 0, row = 0, expectedVersion = state.Version });
        var version = (await first.Content.ReadFromJsonAsync<TurnDto>(Json))!.State.Version;

        var second = await Fire(state.Id, new { column = 0, row = 0, expectedVersion = version });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("AlreadyTargeted", (await Body(second)).GetProperty("rejection").GetString());
    }

    [Fact]
    public async Task La_grille_adverse_ne_revele_que_la_case_tiree()
    {
        // Vérifie les DTO typés ; le test sur le JSON brut est prévu en S7.
        var state = await CreateStartedGame();
        Assert.Empty(state.Opponent.Misses.Concat(state.Opponent.Hits).Concat(state.Opponent.SunkShips.SelectMany(cells => cells)));

        var response = await Fire(state.Id, new { column = 0, row = 0, expectedVersion = state.Version });

        var opponent = (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!.State.Opponent;
        var revealed = opponent.Misses.Concat(opponent.Hits).Concat(opponent.SunkShips.SelectMany(cells => cells)).Distinct();
        Assert.Equal([new Coordinate(0, 0)], revealed);
    }
}
