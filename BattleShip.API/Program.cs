using System.Text.Json.Serialization;
using BattleShip.API.Endpoints;
using BattleShip.API.GrpcServices;
using BattleShip.API.Storage;
using BattleShip.API.Validation;
using BattleShip.Grpc;
using BattleShip.Models.Dtos;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
// Enums en chaînes sur le réseau : ajouter une valeur ne décale pas le contrat, et api.http reste lisible.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Singleton : les parties doivent survivre d'une requête à l'autre.
builder.Services.AddSingleton<GameStore>();
builder.Services.AddSingleton(Random.Shared);
builder.Services.AddSingleton<IValidator<FireRequest>, FireRequestValidator>();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IValidator<GetGameRequest>, GetGameRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseGrpcWeb();
app.MapGameEndpoints();
// Le navigateur ne parle pas gRPC natif : le service n'est joignable depuis l'App qu'en gRPC-Web.
app.MapGrpcService<GameGrpcService>().EnableGrpcWeb();

app.Run();

// Rend Program accessible à WebApplicationFactory dans les tests d'endpoints.
public partial class Program
{
}
