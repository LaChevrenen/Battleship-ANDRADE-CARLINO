using BattleShip.API.Engine;
using BattleShip.API.Storage;
using BattleShip.Models;
using BattleShip.Models.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BattleShip.API.Endpoints;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/games");
        games.MapPost("/", Create);
        games.MapGet("/{id:guid}", GetState);
        games.MapPost("/{id:guid}/start", Start);
        games.MapPost("/{id:guid}/shots", Fire);
        return app;
    }

    private static Created<GameStateDto> Create(GameStore store, Random random)
    {
        var state = store.Add(Game.CreateWithRandomFleets(random), GameDtoMapper.ToStateDto);
        return TypedResults.Created($"/games/{state.Id}", state);
    }

    private static Results<Ok<GameStateDto>, NotFound> GetState(Guid id, GameStore store) =>
        store.TryExecute(id, GameDtoMapper.ToStateDto, out var state)
            ? TypedResults.Ok(state)
            : TypedResults.NotFound();

    private static Results<Ok<TurnDto>, NotFound, Conflict<ProblemDetails>> Start(Guid id, GameStore store, Random random)
    {
        if (!store.TryExecute(id, stored => StartLocked(stored, random), out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static async Task<Results<Ok<TurnDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> Fire(
        Guid id, FireRequest request, IValidator<FireRequest> validator, GameStore store, Random random)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var target = new Coordinate(request.Column!.Value, request.Row!.Value);
        if (!store.TryExecute(id, stored => FireLocked(stored, target, request.ExpectedVersion!.Value, random), out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static Results<Ok<TurnDto>, NotFound, Conflict<ProblemDetails>> StartLocked(StoredGame stored, Random random)
    {
        if (!stored.TryStart(ChooseComputerTarget(random), out var computerShots))
            return TypedResults.Conflict(Rejection("AlreadyStarted", "La partie a déjà commencé."));

        return TypedResults.Ok(GameDtoMapper.ToOpeningTurnDto(computerShots, stored));
    }

    private static Results<Ok<TurnDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>> FireLocked(
        StoredGame stored, Coordinate target, int expectedVersion, Random random)
    {
        if (!stored.TryFire(target, expectedVersion, ChooseComputerTarget(random), out var turn))
            return TypedResults.Conflict(Rejection("StaleVersion", "La partie a changé depuis votre dernier affichage : rechargez-la avant de tirer."));

        return turn.PlayerShot.Rejection switch
        {
            null => TypedResults.Ok(GameDtoMapper.ToTurnDto(turn, stored)),
            // Les bornes dépendent de la partie : c'est le moteur qui les juge, mais c'est bien une entrée invalide.
            ShotRejection.OutOfBounds => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["target"] = [Message(ShotRejection.OutOfBounds)] }),
            ShotRejection reason => TypedResults.Conflict(Rejection(reason.ToString(), Message(reason))),
        };
    }

    // La fonction ne capture que le hasard : l'ordinateur ne voit que la vue que Game lui transmet.
    private static Func<RevealedBoard, Coordinate> ChooseComputerTarget(Random random) =>
        view => HuntTargetStrategy.ChooseTarget(view, random);

    private static ProblemDetails Rejection(string code, string title) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = title,
        Extensions = { ["rejection"] = code },
    };

    private static string Message(ShotRejection reason) => reason switch
    {
        ShotRejection.OutOfBounds => "La case est hors de la grille.",
        ShotRejection.AlreadyTargeted => "Cette case a déjà été tirée.",
        ShotRejection.NotYourTurn => "Ce n'est pas votre tour.",
        ShotRejection.NotStarted => "La partie n'a pas encore commencé.",
        ShotRejection.GameOver => "La partie est terminée.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };
}
