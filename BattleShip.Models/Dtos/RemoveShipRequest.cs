namespace BattleShip.Models.Dtos;

// Retrait par case : le joueur désigne un navire en cliquant dessus, le serveur retrouve lequel.
public sealed record RemoveShipRequest(int? Column, int? Row, int? ExpectedVersion);
