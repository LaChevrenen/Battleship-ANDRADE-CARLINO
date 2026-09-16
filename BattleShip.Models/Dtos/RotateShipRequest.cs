namespace BattleShip.Models.Dtos;

// Rotation par case : le joueur désigne un navire en le survolant, le serveur retrouve lequel et
// le fait pivoter autour de son origine. Forme voisine de RemoveShipRequest, intention différente.
public sealed record RotateShipRequest(int? Column, int? Row, int? ExpectedVersion);
