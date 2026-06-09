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

WebApplication app = builder.Build();

// Автоматически применяем миграции при старте
using (IServiceScope scope = app.Services.CreateScope())
{
    PaymentDbContext db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.MapGrpcService<PaymentGrpcService>();
app.MapGrpcReflectionService();

// Healthcheck — удобно для docker-compose depends_on
app.MapGet("/health", () => Results.Ok("Payment Service is running"));

app.Run();
