using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BattleShip.App;
using BattleShip.App.Services;
using BattleShip.Protocol;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiAddress = builder.Configuration["ApiAddress"]
    ?? throw new InvalidOperationException("ApiAddress est absent de wwwroot/appsettings.json.");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiAddress) });
builder.Services.AddScoped<GameApiClient>();
// Une seule instance pour toute la session : la musique traverse les changements de page.
builder.Services.AddScoped<GameAudio>();
// GrpcWebHandler : le navigateur ne sait pas parler gRPC natif, il envoie du gRPC-Web sur HTTP/1.1.
builder.Services.AddScoped(sp => new GameService.GameServiceClient(
    GrpcChannel.ForAddress(apiAddress, new GrpcChannelOptions
    {
        HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
    })));

await builder.Build().RunAsync();
