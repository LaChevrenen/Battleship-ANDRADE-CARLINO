using BattleShip.API.Engine;
using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

// Contrairement aux autres validateurs du projet, les bornes SONT jugées ici : elles ne dépendent
// d'aucune partie en cours (rien n'existe encore au moment de la création), donc rien n'oblige à
// les déléguer au moteur. Seule la question « cette configuration tient-elle vraiment sur la
// grille ? » reste du ressort du moteur (Game.TryCreate), parce qu'elle demande de faire tourner
// l'algorithme de placement.
public sealed class GameCreationRequestValidator : AbstractValidator<GameCreationRequest>
{
    public GameCreationRequestValidator()
    {
        RuleFor(request => request.Width)
            .NotNull().WithMessage("La largeur est obligatoire.")
            .InclusiveBetween(GameRules.MinGridSize, GameRules.MaxGridSize)
            .WithMessage($"La largeur doit être comprise entre {GameRules.MinGridSize} et {GameRules.MaxGridSize}.");

        RuleFor(request => request.Height)
            .NotNull().WithMessage("La hauteur est obligatoire.")
            .InclusiveBetween(GameRules.MinGridSize, GameRules.MaxGridSize)
            .WithMessage($"La hauteur doit être comprise entre {GameRules.MinGridSize} et {GameRules.MaxGridSize}.");

        RuleFor(request => request.AllowAdjacentShips)
            .NotNull().WithMessage("Le réglage de contact entre navires est obligatoire.");

        RuleFor(request => request.SpecialAttacksEnabled)
            .NotNull().WithMessage("Le réglage des attaques spéciales est obligatoire.");

        RuleFor(request => request.ShipCounts)
            .NotNull().WithMessage("La composition de la flotte est obligatoire.");

        When(request => request.ShipCounts is not null, () =>
        {
            RuleForEach(request => request.ShipCounts!).ChildRules(count =>
            {
                count.RuleFor(pair => pair.Key)
                    .InclusiveBetween(GameRules.MinShipLength, GameRules.MaxShipLength)
                    .WithMessage($"Une longueur de navire doit être comprise entre {GameRules.MinShipLength} "
                        + $"et {GameRules.MaxShipLength}.");

                count.RuleFor(pair => pair.Value)
                    .InclusiveBetween(0, GameRules.MaxShipsPerLength)
                    .WithMessage($"Le nombre de navires d'une même longueur doit être compris entre 0 et "
                        + $"{GameRules.MaxShipsPerLength}.");
            });

            RuleFor(request => request.ShipCounts!.Values.Sum())
                .GreaterThan(0).WithMessage("La flotte doit contenir au moins un navire.");
        });
    }
}
