namespace BattleShip.Models.Dtos;

// Corps des opérations de préparation sans autre donnée : retirer le dernier navire, tirer la flotte au hasard.
public sealed record VersionedRequest(int? ExpectedVersion);
