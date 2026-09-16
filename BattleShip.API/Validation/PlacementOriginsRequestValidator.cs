using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class PlacementOriginsRequestValidator : AbstractValidator<PlacementOriginsRequest>
{
    public PlacementOriginsRequestValidator()
    {
        RuleFor(request => request.Length)
            .NotNull().WithMessage("La longueur du navire est obligatoire.")
            .GreaterThan(0).WithMessage("La longueur du navire doit être positive.");
        RuleFor(request => request.Orientation)
            .NotNull().WithMessage("L'orientation est obligatoire.")
            .IsInEnum().WithMessage("L'orientation doit être Horizontal ou Vertical.");
    }
}
