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
            [.. state.Opponent.SunkShips.Select(ToCells)]));

    private static IReadOnlyList<Coordinate> ToCells(Proto.CellList ship) => [.. ship.Cells.Select(ToDto)];

    private static Coordinate ToDto(Proto.Coordinate cell) => new(cell.Column, cell.Row);

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
}
