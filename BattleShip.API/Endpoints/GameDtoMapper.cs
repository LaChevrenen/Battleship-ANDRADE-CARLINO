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
            // Vue révélée pour ce qui a été joué ; RemainingEnemyShips n'ajoute les navires jamais
            // coulés que si la partie est perdue, seule brèche volontaire dans l'invariant n°1.
            ToOpponentBoardDto(game.ComputerBoard.Reveal(), RemainingEnemyShips(game)),
            ToStatistics(stored),
            game.Difficulty,
            game.AllowAdjacentShips,
            GameRules.SpecialAttackChargeInterval,
            game.SpecialAttackProgress(Side.Player),
            game.SpecialAttackProgress(Side.Computer),
            game.SpecialAttacksEnabled);
    }

    public static TurnDto ToTurnDto(PlayerTurnResult turn, StoredGame stored) =>
        new(turn.PlayerShot.Outcome,
            turn.PlayerShot.SunkShip?.Cells.ToList(),
            [.. turn.ComputerShots.Select(ToComputerShotDto)],
            ToStateDto(stored));

    public static TurnDto ToOpeningTurnDto(IReadOnlyList<ComputerShot> computerShots, StoredGame stored) =>
        new(null, null, [.. computerShots.Select(ToComputerShotDto)], ToStateDto(stored));

    // Même forme que ToTurnDto : une attaque spéciale se lit à la même adresse, avec un résultat
    // agrégé (AreaShotResult.AggregateOutcome) au lieu d'un résultat unique.
    public static TurnDto ToAreaTurnDto(PlayerAreaTurnResult turn, StoredGame stored) =>
        new(turn.PlayerShot.AggregateOutcome,
            turn.PlayerShot.SunkShip?.Cells.ToList(),
            [.. turn.ComputerShots.Select(ToComputerShotDto)],
            ToStateDto(stored));

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

    private static OpponentBoardDto ToOpponentBoardDto(RevealedBoard view, IReadOnlyList<IReadOnlyList<Coordinate>> remainingShips) =>
        new(view.Width,
            view.Height,
            [.. view.Misses],
            [.. view.Hits],
            [.. view.SunkShips.Select(cells => cells.ToList())],
            remainingShips);

    // Défaite : la partie est finie, le joueur ne peut plus rien tenter contre ces navires, il n'y
    // a donc plus rien à protéger derrière leur position. Vide dans tous les autres cas (partie en
    // cours, ou terminée par une victoire — où tout est de toute façon déjà coulé).
    private static IReadOnlyList<IReadOnlyList<Coordinate>> RemainingEnemyShips(Game game) =>
        game.Phase == GamePhase.Finished && game.Winner == Side.Computer
            ? [.. game.ComputerBoard.UnsunkShips().Select(ship => ship.Cells.ToList())]
            : [];

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
        new(shot.Target, shot.Result.Outcome!.Value, shot.SpecialAttack);
}
