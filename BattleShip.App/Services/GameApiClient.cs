using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public async Task<TurnDto> StartAsync(Guid id)
    {
        var response = await http.PostAsync($"/games/{id}/start", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TurnDto>(Json))!;
    }

    public async Task<FireResult> FireAsync(Guid id, int column, int row, int expectedVersion)
    {
        var response = await http.PostAsJsonAsync($"/games/{id}/shots", new FireRequest(column, row, expectedVersion), Json);
        if (response.IsSuccessStatusCode)
            return new FireResult((await response.Content.ReadFromJsonAsync<TurnDto>(Json))!, null, MustReload: false);

        // Refus du serveur : on n'affiche que ce qu'il dit, sans rien deviner de l'état de la partie.
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rejection = problem.TryGetProperty("rejection", out var code) ? code.GetString() : null;
        var title = problem.TryGetProperty("title", out var text) ? text.GetString() : response.StatusCode.ToString();

        return new FireResult(null, title, MustReload: rejection == "StaleVersion" || response.StatusCode == HttpStatusCode.NotFound);
    }
}
