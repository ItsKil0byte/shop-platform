using BuildingBlocks.Consul;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Infrastructure.GrpcServer;
using Payment.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// БД
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Репозиторий и бизнес-логика
builder.Services.AddScoped<IPaymentRepository, EFPaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// gRPC сервер
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddHealthChecks();
builder.Services.AddGrpcHealthChecks()
    .AddCheck("payment", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Consul
builder.Services.AddConsul(builder.Configuration);

WebApplication app = builder.Build();

// Миграции
using (IServiceScope scope = app.Services.CreateScope())
{
    PaymentDbContext db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.MapGrpcService<PaymentGrpcService>();
app.MapGrpcHealthChecksService();
app.MapGrpcReflectionService();

app.MapGet("/health", () => Results.Ok("Payment Service is running"));

// Регистрация в Consul
app.UseConsul();

app.Run();
