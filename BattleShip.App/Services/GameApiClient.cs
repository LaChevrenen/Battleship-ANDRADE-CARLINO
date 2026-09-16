using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BattleShip.Models;
using BattleShip.Models.Dtos;

namespace BattleShip.App.Services;

public sealed class GameApiClient(HttpClient http)
{
    // L'API sérialise les enums en chaînes : le client doit utiliser le même convertisseur.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<GameStateDto> CreateAsync()
    {
        var response = await http.PostAsync("/games", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!;
    }

    public async Task<PreparationResult> PlaceShipAsync(Guid id, Coordinate origin, int length, Orientation orientation, int expectedVersion) =>
        await ReadState(await http.PostAsJsonAsync(
            $"/games/{id}/ships",
            new PlaceShipRequest(origin.Column, origin.Row, length, orientation, expectedVersion),
            Json));

    public async Task<PreparationResult> RemoveShipAsync(Guid id, Coordinate cell, int expectedVersion) =>
        await ReadState(await http.PostAsJsonAsync(
            $"/games/{id}/ships/remove",
            new RemoveShipRequest(cell.Column, cell.Row, expectedVersion),
            Json));

    public async Task<IReadOnlyList<Coordinate>> ValidOriginsAsync(Guid id, int length, Orientation orientation)
    {
        var response = await http.GetFromJsonAsync<PlacementOriginsDto>(
            $"/games/{id}/placements?length={length}&orientation={orientation}", Json);
        return response?.Origins ?? [];
    }

    public async Task<PreparationResult> PlaceFleetAtRandomAsync(Guid id, int expectedVersion) =>
        await ReadState(await http.PostAsJsonAsync($"/games/{id}/fleet/random", new VersionedRequest(expectedVersion), Json));

    public async Task<TurnResult> StartAsync(Guid id) =>
        await ReadTurn(await http.PostAsync($"/games/{id}/start", null));

    public async Task<TurnResult> FireAsync(Guid id, int column, int row, int expectedVersion) =>
        await ReadTurn(await http.PostAsJsonAsync($"/games/{id}/shots", new FireRequest(column, row, expectedVersion), Json));

    private static async Task<TurnResult> ReadTurn(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return new TurnResult((await response.Content.ReadFromJsonAsync<TurnDto>(Json))!, null, MustReload: false);

        var (refusal, mustReload) = await ReadProblem(response);
        return new TurnResult(null, refusal, mustReload);
    }

    private static async Task<PreparationResult> ReadState(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return new PreparationResult((await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!, null, MustReload: false);

        var (refusal, mustReload) = await ReadProblem(response);
        return new PreparationResult(null, refusal, mustReload);
    }

    // Refus du serveur : on n'affiche que ce qu'il dit, sans rien deviner de l'état de la partie.
    private static async Task<(string? Refusal, bool MustReload)> ReadProblem(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rejection = problem.TryGetProperty("rejection", out var code) ? code.GetString() : null;
        var title = problem.TryGetProperty("title", out var text) ? text.GetString() : response.StatusCode.ToString();

        // Ces refus disent que l'état affiché n'est plus celui du serveur : il faut le relire avant de rejouer.
        var mustReload = rejection is "StaleVersion" or "AlreadyStarted" or "NotInSetup"
            || response.StatusCode == HttpStatusCode.NotFound;

        return (title, mustReload);
    }
}
