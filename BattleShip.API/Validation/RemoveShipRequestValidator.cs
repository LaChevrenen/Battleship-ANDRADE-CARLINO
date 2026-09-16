using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class RemoveShipRequestValidator : AbstractValidator<RemoveShipRequest>
{
    public RemoveShipRequestValidator()
    {
        RuleFor(request => request.Column).NotNull().WithMessage("La colonne est obligatoire.");
        RuleFor(request => request.Row).NotNull().WithMessage("La ligne est obligatoire.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
