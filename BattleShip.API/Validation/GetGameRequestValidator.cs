using BattleShip.Protocol;
using FluentValidation;

namespace BattleShip.API.Validation;

public sealed class GetGameRequestValidator : AbstractValidator<GetGameRequest>
{
    public GetGameRequestValidator()
    {
        // Stop : un identifiant vide ne doit produire qu'une erreur, pas aussi « format invalide ».
        RuleFor(request => request.GameId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("L'identifiant de la partie est obligatoire.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("L'identifiant de la partie n'est pas valide.");
    }
}
