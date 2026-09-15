using System.Net.Http.Json;
using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BattleShip.Tests.Api;

// Invariant n°1 vérifié sur la chaîne JSON renvoyée, pas sur un objet désérialisé :
// un champ inconnu du DTO de test ou une fuite hors de la grille adverse y restent visibles.
public sealed class MaskingJsonTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Graine choisie une fois, à ne plus changer : avec elle, le joueur commence et l'ordinateur ne tire pas.
    // Le test le vérifie avant toute recherche, sinon des tirs de l'ordinateur pourraient fausser le résultat.
    private const int PlayerStartsSeed = 1;

    private static readonly Coordinate HitCell = new(6, 1);

    private readonly HttpClient client = factory.CreateClient();

    private GameStore Store => factory.Services.GetRequiredService<GameStore>();

    // Joueur dans les colonnes 0 à 4, ordinateur dans les colonnes 5 à 9 : aucune coordonnée commune,
    // donc une case de navire adverse trouvée dans la réponse est une fuite et jamais une case du joueur.
    private Guid AddGameWithSeparatedFleets() =>
        Store.Add(
            new Game(
                new Board(10, 10, [new Ship([new(0, 0), new(0, 1), new(0, 2)]), new Ship([new(2, 5), new(3, 5)])]),
                new Board(10, 10, [new Ship([HitCell, new(7, 1), new(8, 1)]), new Ship([new(9, 6), new(9, 7)])]),
                new Random(PlayerStartsSeed)),
            stored => stored.Id);

    private (List<Coordinate> Player, List<Coordinate> Computer) ShipCellsReadOnServer(Guid id)
    {
        Store.TryExecute(
            id,
            stored => (stored.Game.PlayerBoard.Ships.SelectMany(ship => ship.Cells).ToList(),
                       stored.Game.ComputerBoard.Ships.SelectMany(ship => ship.Cells).ToList()),
            out var cells);
        Assert.Empty(cells.Item1.Intersect(cells.Item2));
        return cells;
    }

    private static string AsJson(Coordinate cell) => $"{{\"column\":{cell.Column},\"row\":{cell.Row}}}";

    private static void AssertNoComputerCellLeaks(string rawJson, IEnumerable<Coordinate> computerCells, params Coordinate[] legitimatelyRevealed)
    {
        foreach (var cell in computerCells.Except(legitimatelyRevealed))
            Assert.DoesNotContain(AsJson(cell), rawJson);
    }

    [Fact]
    public async Task Le_json_d_une_partie_neuve_ne_contient_aucune_case_de_navire_adverse()
    {
        var id = AddGameWithSeparatedFleets();
        var ships = ShipCellsReadOnServer(id);

        var raw = await client.GetStringAsync($"/games/{id}");

        // Garde-fou : le format recherché est bien celui de la réponse, sinon toutes les recherches passeraient à vide.
        Assert.Contains(AsJson(ships.Player[0]), raw);
        AssertNoComputerCellLeaks(raw, ships.Computer);
    }

    [Fact]
    public async Task Le_json_d_une_partie_en_cours_ne_revele_que_la_case_touchee_d_un_navire_non_coule()
    {
        var id = AddGameWithSeparatedFleets();
        var ships = ShipCellsReadOnServer(id);

        var startRaw = await (await client.PostAsync($"/games/{id}/start", null)).Content.ReadAsStringAsync();
        Assert.Contains("\"computerShots\":[]", startRaw);
        AssertNoComputerCellLeaks(startRaw, ships.Computer);

        var fireResponse = await client.PostAsJsonAsync($"/games/{id}/shots", new { column = HitCell.Column, row = HitCell.Row, expectedVersion = 1 });
        var fireRaw = await fireResponse.Content.ReadAsStringAsync();
        var stateRaw = await client.GetStringAsync($"/games/{id}");

        Assert.Contains("\"playerOutcome\":\"Hit\"", fireRaw);
        foreach (var raw in new[] { fireRaw, stateRaw })
        {
            Assert.Contains(AsJson(HitCell), raw);
            AssertNoComputerCellLeaks(raw, ships.Computer, HitCell);
        }
    }
}
