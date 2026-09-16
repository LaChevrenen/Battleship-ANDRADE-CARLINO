using BattleShip.API.Endpoints;
using BattleShip.API.Storage;
using BattleShip.Protocol;
using FluentValidation;
using Grpc.Core;

namespace BattleShip.API.GrpcServices;

public sealed class GameGrpcService(GameStore store, IValidator<GetGameRequest> validator) : GameService.GameServiceBase
{
    // En gRPC ASP.NET Core, une erreur attendue se renvoie en levant RpcException : c'est ainsi que le statut atteint le client.
    public override async Task<GameState> GetGame(GetGameRequest request, ServerCallContext context)
    {
        var validation = await validator.ValidateAsync(request, context.CancellationToken);
        if (!validation.IsValid)
            throw new RpcException(new Status(StatusCode.InvalidArgument, validation.Errors[0].ErrorMessage));

        // Même verrou et même traduction que GET /games/{id}.
        if (!store.TryExecute(Guid.Parse(request.GameId), GameDtoMapper.ToStateDto, out var state))
            throw new RpcException(new Status(StatusCode.NotFound, "Partie introuvable : elle n'existe plus sur le serveur."));

        return GameStateMessageMapper.ToMessage(state);
    }
}
