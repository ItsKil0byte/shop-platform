using Moderation.Api.Grpc;
using BuildingBlocks.Logging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging("ModerationService");

builder.Services.AddGrpc();

WebApplication app = builder.Build();

app.MapGrpcService<ModerationGrpcService>();

app.MapGet("/", () => "Ты помоему перепутал.");

app.Run();