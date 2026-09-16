using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

// Comme pour le tir, les bornes de la grille ne sont pas vérifiées ici : le moteur les juge.
public sealed class PlaceShipRequestValidator : AbstractValidator<PlaceShipRequest>
{
    public PlaceShipRequestValidator()
    {
        RuleFor(request => request.Column).NotNull().WithMessage("La colonne est obligatoire.");
        RuleFor(request => request.Row).NotNull().WithMessage("La ligne est obligatoire.");
        RuleFor(request => request.Length)
            .NotNull().WithMessage("La longueur du navire est obligatoire.")
            .GreaterThan(0).WithMessage("La longueur du navire doit être positive.");
        RuleFor(request => request.Orientation)
            .NotNull().WithMessage("L'orientation est obligatoire.")
            .IsInEnum().WithMessage("L'orientation doit être Horizontal ou Vertical.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
