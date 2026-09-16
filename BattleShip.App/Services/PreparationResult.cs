using BattleShip.Models.Dtos;

namespace BattleShip.App.Services;

// Résultat d'une opération de préparation : le nouvel état, ou le motif de refus du serveur.
public sealed record PreparationResult(GameStateDto? State, string? Refusal, bool MustReload);
