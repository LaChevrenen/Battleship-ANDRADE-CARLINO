namespace BattleShip.Models.Dtos;

// Corps de POST /games. Absent (ou null) : partie classique, grille 10x10, flotte par défaut,
// contact interdit. Présent : partie personnalisée, tous les champs sont alors requis — pas de
// mélange entre un réglage choisi et un réglage par défaut au sein d'une même requête.
public sealed record GameCreationRequest(
    int? Width,
    int? Height,
    // Longueur (1 à 5) -> nombre de navires de cette longueur (0 à 3). Une longueur absente du
    // dictionnaire vaut 0.
    IReadOnlyDictionary<int, int>? ShipCounts,
    bool? AllowAdjacentShips,
    bool? SpecialAttacksEnabled);
