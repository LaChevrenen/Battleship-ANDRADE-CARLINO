namespace BattleShip.Models.Dtos;

// Paramètres de la requête qui demande où un navire peut être posé.
public sealed record PlacementOriginsRequest(int? Length, Orientation? Orientation);
