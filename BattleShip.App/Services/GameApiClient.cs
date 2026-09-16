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
}
