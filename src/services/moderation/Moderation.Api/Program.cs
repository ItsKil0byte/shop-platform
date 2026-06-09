using Moderation.Api.Grpc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

WebApplication app = builder.Build();

app.MapGrpcService<ModerationGrpcService>();

app.MapGet("/", () => "Ты помоему перепутал.");

app.Run();