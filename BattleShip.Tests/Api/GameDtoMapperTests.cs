using BattleShip.API.Endpoints;
using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;

namespace BattleShip.Tests.Api;

public sealed class GameDtoMapperTests
{
    [Fact]
    public void Ma_grille_annonce_mes_navires_coules_sans_que_le_client_ait_a_les_deduire()
    {
        var playerShips = new Ship[] { new([new(0, 0), new(1, 0)]), new([new(5, 5)]) };
        var game = new Game(
            FleetUnderConstruction.Placed(10, 10, playerShips),
            new Board(10, 10, [new Ship([new(9, 0)])]),
            new Random(0));
        // Tirs de l'ordinateur sur ma grille : le navire d'une case est coulé, l'autre seulement touché.
        game.PlayerBoard.ReceiveShot(new(5, 5));
        game.PlayerBoard.ReceiveShot(new(0, 0));
        game.PlayerBoard.ReceiveShot(new(9, 9));
        var stored = new StoredGame(Guid.NewGuid(), game);

        var state = GameDtoMapper.ToStateDto(stored);

        var sunk = Assert.Single(state.Player.SunkShips);
        Assert.Equal<Coordinate>([new(5, 5)], sunk);
        Assert.Equal<Coordinate>(
            [new(0, 0), new(5, 5)],
            [.. state.Player.Hits.OrderBy(cell => cell.Column).ThenBy(cell => cell.Row)]);
        Assert.Equal<Coordinate>([new(9, 9)], state.Player.Misses);
    }
}
