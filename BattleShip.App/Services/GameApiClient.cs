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

    public async Task<CreationResult> CreateAsync()
    {
        var response = await http.PostAsync("/games", null);
        if (!response.IsSuccessStatusCode)
            return new CreationResult(null, (await ReadProblem(response)).Refusal);

        return new CreationResult((await response.Content.ReadFromJsonAsync<GameStateDto>(Json))!, null);
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

    // GetFromJsonAsync lèverait sur une réponse d'erreur, et l'exception remonterait jusqu'au
    // rendu du composant : comme pour le tir et le placement, un refus se lit, il ne se jette pas.
    public async Task<OriginsResult> ValidOriginsAsync(Guid id, int length, Orientation orientation)
    {
        var response = await http.GetAsync($"/games/{id}/placements?length={length}&orientation={orientation}");
        if (!response.IsSuccessStatusCode)
            return new OriginsResult([], (await ReadProblem(response)).Refusal);

        var origins = await response.Content.ReadFromJsonAsync<PlacementOriginsDto>(Json);
        return new OriginsResult(origins?.Origins ?? [], null);
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
        var title = response.StatusCode.ToString();

        // Ces refus disent que l'état affiché n'est plus celui du serveur : il faut le relire avant de rejouer.
        var mustReload = response.StatusCode == HttpStatusCode.NotFound;

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (problem.TryGetProperty("title", out var text) && text.GetString() is { } read)
                title = read;

            var rejection = problem.TryGetProperty("rejection", out var code) ? code.GetString() : null;
            mustReload = mustReload || rejection is "StaleVersion" or "AlreadyStarted" or "NotInSetup";
        }
        catch (Exception failure) when (failure is JsonException or NotSupportedException)
        {
            // Toutes les erreurs ne portent pas un ProblemDetails : un 500 peut n'avoir aucun corps.
            // Il reste le code de statut, et surtout la page ne casse pas.
        }

        return (title, mustReload);
    }
}
