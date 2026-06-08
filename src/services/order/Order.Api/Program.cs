using Order.Application.Interfaces;
using Order.Application.Services;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.GrpcClients;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Регистрация контроллеров

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация зависимостей

builder.Services.AddDbContext<OrderDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
builder.Services.AddSingleton<IEventPublisher, ConsoleEventPublisher>();

// Регистрация бизнес-логики

builder.Services.AddScoped<IOrderService, OrderService>();

// Регистрация gRPC клиентов

builder.Services.AddScoped<ICartClient, CartGrpcClient>();
builder.Services.AddScoped<IModerationClient, ModerationGrpcClient>();
builder.Services.AddScoped<IPaymentClient, PaymentGrpcClient>();

builder.Services.AddGrpcClient<Moderation.Grpc.ModerationService.ModerationServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5001");
});

builder.Services.AddGrpcClient<Cart.Grpc.CartService.CartServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5002");
});

builder.Services.AddGrpcClient<Payment.Grpc.PaymentService.PaymentServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:5003");
});

// Конфигурация приложения

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();