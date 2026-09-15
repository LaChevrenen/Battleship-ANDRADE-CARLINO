namespace BattleShip.Models.Dtos;

// Champs nullables : un champ absent du JSON doit être refusé, pas lu comme 0.
public sealed record FireRequest(int? Column, int? Row, int? ExpectedVersion);
