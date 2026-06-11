using Order.Application.Interfaces;
using Order.Application.Services;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Messaging;
using Order.Infrastructure.GrpcClients;
using BuildingBlocks.Logging;
using BuildingBlocks.Consul;

using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Подключаем логирование

builder.ConfigureLogging("OrderService");

// Подключаем Consul

builder.Services.AddConsul(builder.Configuration);

// Регистрация контроллеров

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация зависимостей

builder.Services.AddDbContext<OrderDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();
string rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
builder.Services.AddSingleton<IEventPublisher>(_ => new RabbitMqEventPublisher(rabbitHost));

// Регистрация бизнес-логики

builder.Services.AddScoped<IOrderService, OrderService>();

// Регистрация gRPC клиентов

builder.Services.AddScoped<ICartClient, CartGrpcClient>();
builder.Services.AddScoped<IModerationClient, ModerationGrpcClient>();
builder.Services.AddScoped<IPaymentClient, PaymentGrpcClient>();

builder.Services.AddGrpcClient<Moderation.Grpc.ModerationService.ModerationServiceClient>(options =>
{
    string address = builder.Configuration["GrpcClients:ModerationUrl"] ?? "http://localhost:5081";
    options.Address = new Uri(address);
});

builder.Services.AddGrpcClient<Cart.Grpc.CartService.CartServiceClient>(options =>
{
    string address = builder.Configuration["GrpcClients:CartUrl"] ?? "http://localhost:5082";
    options.Address = new Uri(address);
});

builder.Services.AddGrpcClient<Payment.Grpc.PaymentService.PaymentServiceClient>(options =>
{
    string address = builder.Configuration["GrpcClients:PaymentUrl"] ?? "http://localhost:5083";
    options.Address = new Uri(address);
});

// Конфигурация приложения

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    OrderDBContext db = scope.ServiceProvider.GetRequiredService<OrderDBContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseConsul();

app.UseAuthorization();
app.MapControllers();

app.Run();