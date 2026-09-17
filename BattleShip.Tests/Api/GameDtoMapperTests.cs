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

    [Fact]
    public void La_grille_adverse_ne_revele_rien_en_cas_de_victoire()
    {
        var game = new Game(
            FleetUnderConstruction.Placed(10, 10, [new Ship([new(9, 9)])]),
            new Board(10, 10, [new Ship([new(0, 0)])]),
            new Random(0));

        // Case vide de mon plateau : si l'ordinateur commence, son tir rate et me rend la main.
        Coordinate ChooseComputerTarget(RevealedBoard _) => new(9, 8);

        game.TryStart(ChooseComputerTarget, out _);
        // Coule l'unique navire adverse, quel que soit qui a commencé : la main est à moi ici.
        game.PlayerFire(new(0, 0), ChooseComputerTarget);

        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Player, game.Winner);

        var stored = new StoredGame(Guid.NewGuid(), game);
        var state = GameDtoMapper.ToStateDto(stored);

        Assert.Empty(state.Opponent.RemainingShips);
    }

    [Fact]
    public void La_grille_adverse_revele_les_navires_non_coules_en_cas_de_defaite()
    {
        var playerShip = new Ship([new(0, 0)]);
        Ship[] computerShips = [new([new(5, 5)]), new([new(8, 8), new(8, 9)])];
        var game = new Game(FleetUnderConstruction.Placed(10, 10, [playerShip]), new Board(10, 10, computerShips), new Random(0));

        // Cible fixe : la seule case du joueur. Que l'ordinateur commence ou que je doive d'abord
        // rater un tir pour lui rendre la main, son prochain tir la coule et termine la partie.
        Coordinate ChooseComputerTarget(RevealedBoard _) => new(0, 0);

        game.TryStart(ChooseComputerTarget, out _);
        if (game.Phase != GamePhase.Finished)
            game.PlayerFire(new(0, 1), ChooseComputerTarget);

        Assert.Equal(GamePhase.Finished, game.Phase);
        Assert.Equal(Side.Computer, game.Winner);

        var stored = new StoredGame(Guid.NewGuid(), game);
        var state = GameDtoMapper.ToStateDto(stored);

        var revealed = state.Opponent.RemainingShips.Select(ship => ship.ToHashSet()).ToList();
        Assert.Equal(2, revealed.Count);
        Assert.Contains(revealed, ship => ship.SetEquals([new Coordinate(5, 5)]));
        Assert.Contains(revealed, ship => ship.SetEquals([new Coordinate(8, 8), new Coordinate(8, 9)]));
    }
}
