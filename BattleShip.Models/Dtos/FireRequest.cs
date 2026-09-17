namespace BattleShip.Models.Dtos;

// Champs nullables : un champ absent du JSON doit être refusé, pas lu comme 0. SpecialAttack fait
// exception : c'est un réglage, pas une coordonnée, et son absence doit se lire comme false pour
// ne pas casser les clients qui ne le connaissent pas encore.
public sealed record FireRequest(int? Column, int? Row, int? ExpectedVersion, bool SpecialAttack = false);
