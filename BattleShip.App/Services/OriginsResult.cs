using BattleShip.Models;

namespace BattleShip.App.Services;

// Origines où le navire choisi tient, ou le motif de refus du serveur. Une liste vide et un refus
// ne disent pas la même chose : « aucune case libre » se colore en rouge, « je n'ai pas pu
// demander » ne doit rien colorer du tout.
public sealed record OriginsResult(IReadOnlyList<Coordinate> Origins, string? Refusal);
