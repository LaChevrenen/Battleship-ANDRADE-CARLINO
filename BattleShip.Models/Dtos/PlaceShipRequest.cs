namespace BattleShip.Models.Dtos;

// Le client envoie l'origine, la longueur et l'orientation : c'est le serveur qui calcule les cases,
// sinon la règle « un navire occupe une ligne droite » serait vérifiée des deux côtés.
public sealed record PlaceShipRequest(int? Column, int? Row, int? Length, Orientation? Orientation, int? ExpectedVersion);
