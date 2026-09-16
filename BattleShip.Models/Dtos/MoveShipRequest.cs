namespace BattleShip.Models.Dtos;

public sealed record MoveShipRequest(
    int? SourceColumn,
    int? SourceRow,
    int? TargetColumn,
    int? TargetRow,
    Orientation? Orientation,
    int? ExpectedVersion);
