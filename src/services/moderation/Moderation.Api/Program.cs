using Moderation.Api.Grpc;
using BuildingBlocks.Logging;
using BuildingBlocks.Consul;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsul(builder.Configuration);
builder.ConfigureLogging("ModerationService");

builder.Services.AddGrpc();

WebApplication app = builder.Build();

app.UseConsul();

app.MapGrpcService<ModerationGrpcService>();

app.MapGet("/", () => "Ты помоему перепутал.");

app.Run();