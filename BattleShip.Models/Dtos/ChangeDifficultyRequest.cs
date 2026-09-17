namespace BattleShip.Models.Dtos;

// Choix du niveau de l'ordinateur, pendant la préparation uniquement.
public sealed record ChangeDifficultyRequest(AiDifficulty? Difficulty, int? ExpectedVersion);
