using System.Net.Http.Json;
using System.Text.Json;
using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Protocol;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
        // La flotte du joueur reste à poser à la création (E0bis), et le contrat gRPC transporte ce qu'il reste.
        Assert.Empty(state.Player.Ships);
        Assert.Equal<int>([2, 3, 3, 4, 5], state.Player.RemainingShipLengths.Order());
        Assert.Empty(state.Opponent.Hits);
    }

    [Fact]
    public async Task L_etat_grpc_web_transporte_la_jauge_d_attaque_speciale()
    {
        // Graine documentée dans GameEndpointsTests, reproduite ici par une partie personnalisée
        // dont les réglages reprennent exactement ceux du socle classique (seule façon d'y activer
        // les attaques spéciales désormais) : l'ordinateur commence et rate en (8,4), puis un tir du
        // joueur en (0,0) est à l'eau et l'ordinateur tire une deuxième fois. Sa jauge est donc
        // garantie non nulle ici, ce qui permet de détecter une jauge adverse oubliée côté câble.
        // Rejoué en vrai contre une instance de test pour confirmer que la graine se comporte encore
        // à l'identique une fois passée par la création personnalisée plutôt que par POST /games.
        const int seed = 20260915;
        // Le client gRPC doit viser ce même hôte à graine fixe : factory.CreateGrpcClient() vise le
        // hôte partagé de la classe, dont le magasin de parties en mémoire est distinct de celui-ci.
        var seededFactory = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton(new Random(seed))));
        var http = seededFactory.CreateClient();

        var creationBody = new
        {
            width = 10,
            height = 10,
            shipCounts = new Dictionary<int, int> { [5] = 1, [4] = 1, [3] = 2, [2] = 1 },
            allowAdjacentShips = false,
            specialAttacksEnabled = true,
        };
        var created = await (await http.PostAsJsonAsync("/games", creationBody)).Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetInt32();

        var placed = await (await http.PostAsJsonAsync(
            $"/games/{id}/fleet/random", new { expectedVersion = version })).Content.ReadFromJsonAsync<JsonElement>();
        version = placed.GetProperty("version").GetInt32();

        var started = await (await http.PostAsync($"/games/{id}/start", null)).Content.ReadFromJsonAsync<JsonElement>();
        version = started.GetProperty("state").GetProperty("version").GetInt32();

        // Cinq tirs acceptés du joueur : touché ou raté n'a pas d'importance, seule la jauge
        // nous intéresse ici. Le premier, en (0,0), est le tir à l'eau garanti par la graine.
        int[,] cells = { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } };
        for (var i = 0; i < cells.GetLength(0); i++)
        {
            var response = await http.PostAsJsonAsync(
                $"/games/{id}/shots", new { column = cells[i, 0], row = cells[i, 1], expectedVersion = version });
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            version = body.GetProperty("state").GetProperty("version").GetInt32();
        }

        var grpcClient = new GameService.GameServiceClient(GrpcChannel.ForAddress(seededFactory.Server.BaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, seededFactory.Server.CreateHandler()),
        }));
        var state = await grpcClient.GetGameAsync(new GetGameRequest { GameId = id });

        Assert.Equal(GameRules.SpecialAttackChargeInterval, state.SpecialAttackChargeInterval);
        Assert.Equal(GameRules.SpecialAttackChargeInterval, state.PlayerSpecialAttackProgress);
        // L'ordinateur ne joue pas forcément à chaque tour du joueur (il perd son tour sur un tir
        // manqué du joueur mais le garde tant qu'il touche) : seule la borne haute est garantie en
        // général. La borne basse de 1 est garantie par la graine ci-dessus.
        Assert.InRange(state.ComputerSpecialAttackProgress, 1, GameRules.SpecialAttackChargeInterval);
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
