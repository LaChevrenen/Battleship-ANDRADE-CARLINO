using BattleShip.Models.Dtos;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class VersionedRequestValidator : AbstractValidator<VersionedRequest>
{
    public VersionedRequestValidator()
    {
        RuleFor(request => request.ExpectedVersion)
            .NotNull().WithMessage("La version attendue est obligatoire.")
            .GreaterThanOrEqualTo(0).WithMessage("La version attendue ne peut pas être négative.");
    }
}
