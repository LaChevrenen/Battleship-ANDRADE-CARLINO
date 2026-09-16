using BattleShip.Models.Dtos;

namespace BattleShip.App.Services;

// Partie créée, ou motif de refus du serveur.
public sealed record CreationResult(GameStateDto? Game, string? Refusal);
