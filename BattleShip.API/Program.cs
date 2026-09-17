using System.Text.Json.Serialization;
using BattleShip.API.Endpoints;
using BattleShip.API.GrpcServices;
using BattleShip.API.Storage;
using BattleShip.API.Validation;
using BattleShip.Models.Dtos;
using BattleShip.Protocol;
using FluentValidation;

const string FrontPolicy = "front";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy(FrontPolicy, policy => policy
    .WithOrigins("https://localhost:7172", "http://localhost:5274")
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Sans ces en-têtes exposés, le navigateur cache le statut gRPC au code de l'App :
    // une partie introuvable arriverait comme une erreur générique au lieu de NotFound.
    .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));
// Enums en chaînes sur le réseau : ajouter une valeur ne décale pas le contrat, et api.http reste lisible.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Singleton : les parties doivent survivre d'une requête à l'autre.
builder.Services.AddSingleton<GameStore>();
builder.Services.AddSingleton(Random.Shared);
builder.Services.AddSingleton<IValidator<FireRequest>, FireRequestValidator>();
builder.Services.AddSingleton<IValidator<PlaceShipRequest>, PlaceShipRequestValidator>();
builder.Services.AddSingleton<IValidator<VersionedRequest>, VersionedRequestValidator>();
builder.Services.AddSingleton<IValidator<RemoveShipRequest>, RemoveShipRequestValidator>();
builder.Services.AddSingleton<IValidator<RotateShipRequest>, RotateShipRequestValidator>();
builder.Services.AddSingleton<IValidator<ChangeDifficultyRequest>, ChangeDifficultyRequestValidator>();
builder.Services.AddSingleton<IValidator<MoveShipRequest>, MoveShipRequestValidator>();
builder.Services.AddSingleton<IValidator<PlacementOriginsRequest>, PlacementOriginsRequestValidator>();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IValidator<GetGameRequest>, GetGameRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
// Ordre imposé : UseGrpcWeb traduit la requête du navigateur, UseCors répond ensuite sur la requête traduite.
app.UseGrpcWeb();
app.UseCors(FrontPolicy);
app.MapGameEndpoints();
// Le navigateur ne parle pas gRPC natif : le service n'est joignable depuis l'App qu'en gRPC-Web.
// CORS vient de UseCors(FrontPolicy) ci-dessus, qui s'applique à toutes les requêtes.
app.MapGrpcService<GameGrpcService>().EnableGrpcWeb();

app.Run();

// Rend Program accessible à WebApplicationFactory dans les tests d'endpoints.
public partial class Program
{
}
