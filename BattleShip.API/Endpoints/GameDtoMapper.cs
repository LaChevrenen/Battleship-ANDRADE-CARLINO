using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;
using BattleShip.Models.Dtos;

namespace BattleShip.API.Endpoints;

public static class GameDtoMapper
{
    public static GameStateDto ToStateDto(StoredGame stored)
    {
        var game = stored.Game;
        return new GameStateDto(
            stored.Id,
            stored.Version,
            game.Phase,
            game.CurrentTurn,
            game.Winner,
            ToOwnBoardDto(game.PlayerBoard, game.RemainingShipLengths),
            // Seul accès de l'API à la grille adverse : sa vue révélée, jamais ses navires.
            ToOpponentBoardDto(game.ComputerBoard.Reveal()),
            ToStatistics(stored),
            game.Difficulty,
            game.AllowAdjacentShips);
    }

    public static TurnDto ToTurnDto(PlayerTurnResult turn, StoredGame stored) =>
        new(turn.PlayerShot.Outcome,
            turn.PlayerShot.SunkShip?.Cells.ToList(),
            [.. turn.ComputerShots.Select(ToComputerShotDto)],
            ToStateDto(stored));

    public static TurnDto ToOpeningTurnDto(IReadOnlyList<ComputerShot> computerShots, StoredGame stored) =>
        new(null, null, [.. computerShots.Select(ToComputerShotDto)], ToStateDto(stored));

    private static OwnBoardDto ToOwnBoardDto(Board board, IReadOnlyList<int> remainingShipLengths)
    {
        var computerShots = board.Reveal();
        return new OwnBoardDto(
            computerShots.Width,
            computerShots.Height,
            [.. board.Ships.Select(ship => ship.Cells.ToList())],
            [.. computerShots.Misses],
            [.. computerShots.Hits],
            // Mes navires coulés viennent de la vue : la règle « coulé » reste côté serveur.
            [.. computerShots.SunkShips.Select(cells => cells.ToList())],
            [.. remainingShipLengths]);
    }

    private static OpponentBoardDto ToOpponentBoardDto(RevealedBoard view) =>
        new(view.Width,
            view.Height,
            [.. view.Misses],
            [.. view.Hits],
            [.. view.SunkShips.Select(cells => cells.ToList())]);

    private static GameStatisticsDto ToStatistics(StoredGame stored)
    {
        var total = stored.PlayerShotHistory.Count;
        var successful = stored.PlayerShotHistory.Count(shot => shot.Outcome is ShotOutcome.Hit or ShotOutcome.Sunk);
        int? duration = stored.StartedAt is not { } startedAt
            ? null
            : Math.Max(0, (int)((stored.FinishedAt ?? DateTimeOffset.UtcNow) - startedAt).TotalSeconds);

        return new GameStatisticsDto(
            total,
            successful,
            total - successful,
            total == 0 ? 0 : (int)Math.Round(successful * 100d / total),
            duration,
            [.. stored.PlayerShotHistory]);
    }

    // Game ne transmet que les tirs acceptés de l'ordinateur : leur résultat est toujours renseigné.
    private static ComputerShotDto ToComputerShotDto(ComputerShot shot) =>
        new(shot.Target, shot.Result.Outcome!.Value);
}
