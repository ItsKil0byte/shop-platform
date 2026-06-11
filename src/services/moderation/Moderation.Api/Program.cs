using Moderation.Api.Grpc;
using BuildingBlocks.Logging;
using BuildingBlocks.Consul;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsul(builder.Configuration);
builder.ConfigureLogging("ModerationService");

builder.Services.AddGrpc();

builder.Services.AddGrpcHealthChecks()
    .AddCheck("moderation", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

WebApplication app = builder.Build();

app.UseConsul();

app.MapGrpcService<ModerationGrpcService>();
app.MapGrpcHealthChecksService();

app.MapGet("/", () => "Ты помоему перепутал.");

app.Run();