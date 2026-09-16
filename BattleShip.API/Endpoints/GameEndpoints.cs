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
        games.MapGet("/{id:guid}/placements", ValidOrigins);
        games.MapPost("/{id:guid}/ships", PlaceShip);
        // POST et non DELETE : le corps porte la case visée et la version attendue.
        games.MapPost("/{id:guid}/ships/remove", RemoveShip);
        games.MapPost("/{id:guid}/ships/rotate", RotateShip);
        games.MapPost("/{id:guid}/ships/move", MoveShip);
        games.MapPost("/{id:guid}/fleet/random", PlaceFleetAtRandom);
        games.MapPost("/{id:guid}/start", Start);
        games.MapPost("/{id:guid}/shots", Fire);
        return app;
    }

    private static Created<GameStateDto> Create(GameStore store, Random random)
    {
        var state = store.Add(Game.CreateWithRandomComputerFleet(random), GameDtoMapper.ToStateDto);
        return TypedResults.Created($"/games/{state.Id}", state);
    }

    private static Results<Ok<GameStateDto>, NotFound> GetState(Guid id, GameStore store) =>
        store.TryExecute(id, GameDtoMapper.ToStateDto, out var state)
            ? TypedResults.Ok(state)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> PlaceShip(
        Guid id, PlaceShipRequest request, IValidator<PlaceShipRequest> validator, GameStore store)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var origin = new Coordinate(request.Column!.Value, request.Row!.Value);
        if (!store.TryExecute(
                id,
                stored => PlaceShipLocked(stored, origin, request.Length!.Value, request.Orientation!.Value, request.ExpectedVersion!.Value),
                out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static async Task<Results<Ok<PlacementOriginsDto>, ValidationProblem, NotFound>> ValidOrigins(
        Guid id, [AsParameters] PlacementOriginsRequest request, IValidator<PlacementOriginsRequest> validator, GameStore store)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var length = request.Length!.Value;
        var orientation = request.Orientation!.Value;
        if (!store.TryExecute(id, stored => stored.Game.ValidOrigins(length, orientation), out var origins))
            return TypedResults.NotFound();

        return TypedResults.Ok(new PlacementOriginsDto(length, orientation, origins));
    }

    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> RemoveShip(
        Guid id, RemoveShipRequest request, IValidator<RemoveShipRequest> validator, GameStore store)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var cell = new Coordinate(request.Column!.Value, request.Row!.Value);
        if (!store.TryExecute(
                id,
                stored => stored.TryRemoveShipAt(cell, request.ExpectedVersion!.Value, out var rejection)
                    ? rejection is { } reason
                        ? TypedResults.Conflict(Rejection(reason.ToString(), Message(reason)))
                        : Ok(GameDtoMapper.ToStateDto(stored))
                    : TypedResults.Conflict(StaleVersion()),
                out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> RotateShip(
        Guid id, RotateShipRequest request, IValidator<RotateShipRequest> validator, GameStore store)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var cell = new Coordinate(request.Column!.Value, request.Row!.Value);
        if (!store.TryExecute(
                id,
                stored => stored.TryRotateShipAt(cell, request.ExpectedVersion!.Value, out var rejection)
                    ? rejection is { } reason
                        ? TypedResults.Conflict(Rejection(reason.ToString(), Message(reason)))
                        : Ok(GameDtoMapper.ToStateDto(stored))
                    : TypedResults.Conflict(StaleVersion()),
                out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> MoveShip(
        Guid id, MoveShipRequest request, IValidator<MoveShipRequest> validator, GameStore store)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var sourceCell = new Coordinate(request.SourceColumn!.Value, request.SourceRow!.Value);
        var targetOrigin = new Coordinate(request.TargetColumn!.Value, request.TargetRow!.Value);
        if (!store.TryExecute(
                id,
                stored => stored.TryMoveShipAt(sourceCell, targetOrigin, request.Orientation!.Value, request.ExpectedVersion!.Value, out var rejection)
                    ? rejection is { } reason
                        ? TypedResults.Conflict(Rejection(reason.ToString(), Message(reason)))
                        : Ok(GameDtoMapper.ToStateDto(stored))
                    : TypedResults.Conflict(StaleVersion()),
                out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> PlaceFleetAtRandom(
        Guid id, VersionedRequest request, IValidator<VersionedRequest> validator, GameStore store) =>
        await PreparationCommand(id, request, validator, store, (stored, version) =>
            stored.TryPlaceFleetAtRandom(version, out var rejection)
                ? rejection is { } reason
                    ? Rejection(reason.ToString(), Message(reason))
                    : null
                : StaleVersion());

    // Facteur commun des deux commandes sans autre donnée que la version : valider, trouver la partie, agir sous verrou.
    private static async Task<Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>>> PreparationCommand(
        Guid id,
        VersionedRequest request,
        IValidator<VersionedRequest> validator,
        GameStore store,
        Func<StoredGame, int, ProblemDetails?> command)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        if (!store.TryExecute(
                id,
                stored => command(stored, request.ExpectedVersion!.Value) is { } problem
                    ? TypedResults.Conflict(problem)
                    : Ok(GameDtoMapper.ToStateDto(stored)),
                out var result))
            return TypedResults.NotFound();

        return result;
    }

    private static Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>> Ok(GameStateDto state) =>
        TypedResults.Ok(state);

    private static Results<Ok<GameStateDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>> PlaceShipLocked(
        StoredGame stored, Coordinate origin, int length, Orientation orientation, int expectedVersion)
    {
        if (!stored.TryPlaceShip(origin, length, orientation, expectedVersion, out var rejection))
            return TypedResults.Conflict(StaleVersion());

        return rejection switch
        {
            null => TypedResults.Ok(GameDtoMapper.ToStateDto(stored)),
            // Hors grille : la case demandée n'existe pas, comme pour un tir.
            PlacementRejection.OutOfBounds => TypedResults.ValidationProblem(
                new Dictionary<string, string[]> { ["origin"] = [Message(PlacementRejection.OutOfBounds)] }),
            PlacementRejection reason => TypedResults.Conflict(Rejection(reason.ToString(), Message(reason))),
        };
    }

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
        var rejection = stored.TryStart(ChooseComputerTarget(random), out var computerShots);
        if (rejection is not null)
            return TypedResults.Conflict(Rejection(rejection.Value.ToString(), Message(rejection.Value)));

        return TypedResults.Ok(GameDtoMapper.ToOpeningTurnDto(computerShots, stored));
    }

    private static Results<Ok<TurnDto>, ValidationProblem, NotFound, Conflict<ProblemDetails>> FireLocked(
        StoredGame stored, Coordinate target, int expectedVersion, Random random)
    {
        if (!stored.TryFire(target, expectedVersion, ChooseComputerTarget(random), out var turn))
            return TypedResults.Conflict(StaleVersion());

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

    private static ProblemDetails StaleVersion() =>
        Rejection("StaleVersion", "La partie a changé depuis votre dernier affichage : rechargez-la avant de rejouer.");

    private static string Message(PlacementRejection reason) => reason switch
    {
        PlacementRejection.OutOfBounds => "Le navire sort de la grille.",
        PlacementRejection.Overlap => "Ce navire en chevauche un autre.",
        PlacementRejection.AdjacentShip => "Ce navire en touche un autre par un côté.",
        PlacementRejection.LengthNotAvailable => "Tous les navires de cette longueur sont déjà posés.",
        PlacementRejection.NoShipHere => "Aucun navire sur cette case.",
        PlacementRejection.NotInSetup => "La partie a déjà commencé : la flotte ne peut plus changer.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    private static string Message(StartRejection reason) => reason switch
    {
        StartRejection.AlreadyStarted => "La partie a déjà commencé.",
        StartRejection.FleetIncomplete => "Ta flotte n'est pas complète.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
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
