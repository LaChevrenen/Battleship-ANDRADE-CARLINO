using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class ChangeDifficultyRequestValidator : AbstractValidator<ChangeDifficultyRequest>
{
    public ChangeDifficultyRequestValidator()
    {
        // IsInEnum : une valeur hors de l'énumération arrive ici comme un entier valide en C#.
        RuleFor(request => request.Difficulty)
            .NotNull().WithMessage("Le niveau est obligatoire.")
            .IsInEnum().WithMessage("Ce niveau n'existe pas.");
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
