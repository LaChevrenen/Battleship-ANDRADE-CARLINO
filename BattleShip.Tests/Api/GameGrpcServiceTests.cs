using System.Net.Http.Json;
using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Protocol;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BattleShip.Tests.Api;

// Passe par GrpcWebHandler, comme le navigateur : ces tests exercent gRPC-Web, pas le gRPC natif.
public sealed class GameGrpcServiceTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private GameService.GameServiceClient CreateGrpcClient() =>
        new(GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, factory.Server.CreateHandler()),
        }));

    private async Task<string> CreateGameOverHttp()
    {
        var response = await factory.CreateClient().PostAsync("/games", null);
        var created = await response.Content.ReadFromJsonAsync<CreatedGame>();
        return created!.Id;
    }

    private sealed record CreatedGame(string Id);

    [Fact]
    public async Task Lire_une_partie_existante_en_grpc_web_renvoie_son_etat()
    {
        var id = await CreateGameOverHttp();

        var state = await CreateGrpcClient().GetGameAsync(new GetGameRequest { GameId = id });

        Assert.Equal(id, state.Id);
        Assert.Equal(GamePhase.Setup, state.Phase);
        Assert.False(state.HasCurrentTurn);
        // La flotte du joueur reste à poser à la création (E0bis).
        Assert.Empty(state.Player.Ships);
        Assert.Empty(state.Opponent.Hits);
    }

    [Fact]
    public async Task Lire_une_partie_inconnue_en_grpc_web_renvoie_NOT_FOUND()
    {
        var error = await Assert.ThrowsAsync<RpcException>(() =>
            CreateGrpcClient().GetGameAsync(new GetGameRequest { GameId = Guid.NewGuid().ToString() }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, error.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("pas-un-guid")]
    public async Task Un_identifiant_invalide_en_grpc_web_renvoie_INVALID_ARGUMENT(string gameId)
    {
        var error = await Assert.ThrowsAsync<RpcException>(() =>
            CreateGrpcClient().GetGameAsync(new GetGameRequest { GameId = gameId }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task Le_message_grpc_ne_contient_aucune_case_de_navire_adverse_non_touchee()
    {
        // Même scénario que MaskingJsonTests : joueur en colonnes 0 à 4, ordinateur en colonnes 5 à 9,
        // graine 1 où le joueur commence, tir qui touche un navire de 3 cases sans le couler.
        // La case touchée est en ligne 0 exprès : protobuf omet les champs à leur valeur par défaut,
        // donc le garde-fou ne détecte un format tronqué que si la case qu'il cherche contient un 0.
        var store = factory.Services.GetRequiredService<GameStore>();
        var id = store.Add(
            new Game(
                FleetUnderConstruction.Placed(10, 10, [new Ship([new(0, 0), new(0, 1), new(0, 2)]), new Ship([new(2, 5), new(3, 5)])]),
                new Board(10, 10, [new Ship([new(6, 0), new(7, 0), new(8, 0)]), new Ship([new(9, 6), new(9, 7)])]),
                new Random(1)),
            stored => stored.Id);
        var http = factory.CreateClient();
        Assert.Contains("\"computerShots\":[]", await (await http.PostAsync($"/games/{id}/start", null)).Content.ReadAsStringAsync());
        var fire = await http.PostAsJsonAsync($"/games/{id}/shots", new { column = 6, row = 0, expectedVersion = 1 });
        Assert.Contains("\"playerOutcome\":\"Hit\"", await fire.Content.ReadAsStringAsync());
        store.TryExecute(id, stored => stored.Game.ComputerBoard.Ships.SelectMany(ship => ship.Cells).ToList(), out var computerCells);

        var state = await CreateGrpcClient().GetGameAsync(new GetGameRequest { GameId = id.ToString() });
        // Valeurs par défaut incluses : sinon une case en colonne 0 s'écrirait sans son champ column et échapperait à la recherche.
        var json = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatDefaultValues(true)).Format(state);

        Assert.Contains(ProtoJson(6, 0), json);
        foreach (var cell in computerCells.Where(cell => (cell.Column, cell.Row) != (6, 0)))
            Assert.DoesNotContain(ProtoJson(cell.Column, cell.Row), json);
    }

    private static string ProtoJson(int column, int row) => $"{{ \"column\": {column}, \"row\": {row} }}";
}
