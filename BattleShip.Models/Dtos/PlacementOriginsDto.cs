namespace BattleShip.Models.Dtos;

// Cases où ce navire tient, calculées par le serveur : le client s'en sert pour colorer l'aperçu
// sans jamais juger lui-même la validité d'un placement.
public sealed record PlacementOriginsDto(int Length, Orientation Orientation, IReadOnlyList<Coordinate> Origins);
