using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class MoveShipRequestValidator : AbstractValidator<MoveShipRequest>
{
    public MoveShipRequestValidator()
    {
        RuleFor(request => request.SourceColumn).NotNull().WithMessage("La colonne de départ est obligatoire.");
        RuleFor(request => request.SourceRow).NotNull().WithMessage("La ligne de départ est obligatoire.");
        RuleFor(request => request.TargetColumn).NotNull().WithMessage("La colonne d'arrivée est obligatoire.");
        RuleFor(request => request.TargetRow).NotNull().WithMessage("La ligne d'arrivée est obligatoire.");
        RuleFor(request => request.Orientation)
            .NotNull().WithMessage("L'orientation est obligatoire.")
            .IsInEnum().WithMessage("L'orientation doit être Horizontal ou Vertical.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
