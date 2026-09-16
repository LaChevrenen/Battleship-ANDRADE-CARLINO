using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class RotateShipRequestValidator : AbstractValidator<RotateShipRequest>
{
    public RotateShipRequestValidator()
    {
        // Les bornes ne sont pas jugées ici : une case hors grille ne porte aucun navire, et le
        // moteur répond NoShipHere. Comme pour le tir, le validateur ne vérifie que la forme.
        RuleFor(request => request.Column).NotNull().WithMessage("La colonne est obligatoire.");
        RuleFor(request => request.Row).NotNull().WithMessage("La ligne est obligatoire.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
