using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

// Les bornes de la grille ne sont pas vérifiées ici : le moteur les juge avec les dimensions de la partie.
public sealed class FireRequestValidator : AbstractValidator<FireRequest>
{
    public FireRequestValidator()
    {
        RuleFor(request => request.Column).NotNull().WithMessage("La colonne est obligatoire.");
        RuleFor(request => request.Row).NotNull().WithMessage("La ligne est obligatoire.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
