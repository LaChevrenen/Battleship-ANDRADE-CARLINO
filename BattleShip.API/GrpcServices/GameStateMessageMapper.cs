using BattleShip.Models.Dtos;
using Dto = BattleShip.Models;
using Proto = BattleShip.Protocol;

namespace BattleShip.API.GrpcServices;

// Ne reçoit que le DTO : le message gRPC ne peut contenir que ce que la réponse HTTP révèle déjà.
public static class GameStateMessageMapper
{
    public static Proto.GameState ToMessage(GameStateDto state)
    {
        var message = new Proto.GameState
        {
            Id = state.Id.ToString(),
            Version = state.Version,
            Phase = ToMessage(state.Phase),
            Difficulty = ToMessage(state.Difficulty),
            AllowAdjacentShips = state.AllowAdjacentShips,
            SpecialAttackChargeInterval = state.SpecialAttackChargeInterval,
            PlayerSpecialAttackProgress = state.PlayerSpecialAttackProgress,
            ComputerSpecialAttackProgress = state.ComputerSpecialAttackProgress,
            SpecialAttacksEnabled = state.SpecialAttacksEnabled,
            Player = new Proto.OwnBoard
            {
                Width = state.Player.Width,
                Height = state.Player.Height,
                Ships = { state.Player.Ships.Select(ToCellList) },
                Misses = { state.Player.Misses.Select(ToMessage) },
                Hits = { state.Player.Hits.Select(ToMessage) },
                RemainingShipLengths = { state.Player.RemainingShipLengths },
                SunkShips = { state.Player.SunkShips.Select(ToCellList) },
            },
            Opponent = new Proto.OpponentBoard
            {
                Width = state.Opponent.Width,
                Height = state.Opponent.Height,
                Misses = { state.Opponent.Misses.Select(ToMessage) },
                Hits = { state.Opponent.Hits.Select(ToMessage) },
                SunkShips = { state.Opponent.SunkShips.Select(ToCellList) },
            },
            Statistics = new Proto.GameStatistics
            {
                TotalShots = state.Statistics.TotalShots,
                SuccessfulShots = state.Statistics.SuccessfulShots,
                MissedShots = state.Statistics.MissedShots,
                AccuracyPercentage = state.Statistics.AccuracyPercentage,
                History = { state.Statistics.History.Select(ToHistory) },
            },
        };

        if (state.Statistics.DurationSeconds is { } duration)
            message.Statistics.DurationSeconds = duration;

        if (state.CurrentTurn is { } currentTurn)
            message.CurrentTurn = ToMessage(currentTurn);
        if (state.Winner is { } winner)
            message.Winner = ToMessage(winner);

        return message;
    }

    private static Proto.Coordinate ToMessage(Dto.Coordinate cell) => new() { Column = cell.Column, Row = cell.Row };

    private static Proto.CellList ToCellList(IReadOnlyList<Dto.Coordinate> cells) => new() { Cells = { cells.Select(ToMessage) } };

    private static Proto.ShotHistory ToHistory(ShotHistoryDto shot) => new()
    {
        Target = ToMessage(shot.Target),
        Outcome = shot.Outcome switch
        {
            Dto.ShotOutcome.Miss => Proto.ShotOutcome.Miss,
            Dto.ShotOutcome.Hit => Proto.ShotOutcome.Hit,
            Dto.ShotOutcome.Sunk => Proto.ShotOutcome.Sunk,
            _ => throw new ArgumentOutOfRangeException(nameof(shot), shot.Outcome, null),
        },
    };

    // Correspondance explicite et non un transtypage : en proto la valeur 0 est « non renseigné », les numéros diffèrent.
    private static Proto.GamePhase ToMessage(Dto.GamePhase phase) => phase switch
    {
        Dto.GamePhase.Setup => Proto.GamePhase.Setup,
        Dto.GamePhase.InProgress => Proto.GamePhase.InProgress,
        Dto.GamePhase.Finished => Proto.GamePhase.Finished,
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
    };

    private static Proto.Side ToMessage(Dto.Side side) => side switch
    {
        Dto.Side.Player => Proto.Side.Player,
        Dto.Side.Computer => Proto.Side.Computer,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
    };

    private static Proto.AiDifficulty ToMessage(Dto.AiDifficulty difficulty) => difficulty switch
    {
        Dto.AiDifficulty.Easy => Proto.AiDifficulty.Easy,
        Dto.AiDifficulty.Normal => Proto.AiDifficulty.Normal,
        Dto.AiDifficulty.Hard => Proto.AiDifficulty.Hard,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null),
    };
}
