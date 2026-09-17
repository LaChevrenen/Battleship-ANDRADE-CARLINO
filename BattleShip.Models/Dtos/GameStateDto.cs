namespace BattleShip.Models.Dtos;

public sealed record GameStateDto(
    Guid Id,
    int Version,
    GamePhase Phase,
    Side? CurrentTurn,
    Side? Winner,
    OwnBoardDto Player,
    OpponentBoardDto Opponent,
    GameStatisticsDto Statistics,
    AiDifficulty Difficulty,
    bool AllowAdjacentShips,
    // Valeurs brutes plutôt qu'un texte ou un booléen déjà calculé : le client compare lui-même à
    // l'intervalle pour afficher « chargée » ou « encore N tours », sans dupliquer la constante.
    int SpecialAttackChargeInterval,
    int PlayerSpecialAttackProgress,
    int ComputerSpecialAttackProgress);
