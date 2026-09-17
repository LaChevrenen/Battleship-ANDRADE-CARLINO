using BattleShip.Models;
using BattleShip.Models.Dtos;
using Proto = BattleShip.Protocol;

namespace BattleShip.App.Services;

// La page ne connaît qu'une seule forme d'état, celle des DTO : la réponse gRPC est traduite ici.
public static class GameStateConverter
{
    public static GameStateDto ToDto(Proto.GameState state) => new(
        Guid.Parse(state.Id),
        state.Version,
        ToDto(state.Phase),
        state.HasCurrentTurn ? ToDto(state.CurrentTurn) : null,
        state.HasWinner ? ToDto(state.Winner) : null,
        new OwnBoardDto(
            state.Player.Width,
            state.Player.Height,
            [.. state.Player.Ships.Select(ToCells)],
            [.. state.Player.Misses.Select(ToDto)],
            [.. state.Player.Hits.Select(ToDto)],
            [.. state.Player.SunkShips.Select(ToCells)],
            [.. state.Player.RemainingShipLengths]),
        new OpponentBoardDto(
            state.Opponent.Width,
            state.Opponent.Height,
            [.. state.Opponent.Misses.Select(ToDto)],
            [.. state.Opponent.Hits.Select(ToDto)],
            [.. state.Opponent.SunkShips.Select(ToCells)]),
        new GameStatisticsDto(
            state.Statistics.TotalShots,
            state.Statistics.SuccessfulShots,
            state.Statistics.MissedShots,
            state.Statistics.AccuracyPercentage,
            state.Statistics.HasDurationSeconds ? state.Statistics.DurationSeconds : null,
            [.. state.Statistics.History.Select(ToHistory)]),
        ToDto(state.Difficulty),
        state.AllowAdjacentShips,
        state.SpecialAttackChargeInterval,
        state.PlayerSpecialAttackProgress,
        state.ComputerSpecialAttackProgress,
        state.SpecialAttacksEnabled);

    private static IReadOnlyList<Coordinate> ToCells(Proto.CellList ship) => [.. ship.Cells.Select(ToDto)];

    private static Coordinate ToDto(Proto.Coordinate cell) => new(cell.Column, cell.Row);

    private static ShotHistoryDto ToHistory(Proto.ShotHistory shot) => new(ToDto(shot.Target), shot.Outcome switch
    {
        Proto.ShotOutcome.Miss => ShotOutcome.Miss,
        Proto.ShotOutcome.Hit => ShotOutcome.Hit,
        Proto.ShotOutcome.Sunk => ShotOutcome.Sunk,
        _ => throw new ArgumentOutOfRangeException(nameof(shot), shot.Outcome, null),
    });

    // Correspondance explicite : en proto la valeur 0 est « non renseigné », les numéros ne coïncident pas.
    private static GamePhase ToDto(Proto.GamePhase phase) => phase switch
    {
        Proto.GamePhase.Setup => GamePhase.Setup,
        Proto.GamePhase.InProgress => GamePhase.InProgress,
        Proto.GamePhase.Finished => GamePhase.Finished,
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null),
    };

    private static Side ToDto(Proto.Side side) => side switch
    {
        Proto.Side.Player => Side.Player,
        Proto.Side.Computer => Side.Computer,
        _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
    };

    private static AiDifficulty ToDto(Proto.AiDifficulty difficulty) => difficulty switch
    {
        Proto.AiDifficulty.Easy => AiDifficulty.Easy,
        Proto.AiDifficulty.Normal => AiDifficulty.Normal,
        Proto.AiDifficulty.Hard => AiDifficulty.Hard,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null),
    };
}
