using BuildingBlocks.Consul;
using Cart.Api.Grpc;
using Cart.Application.Interfaces;
using Cart.Application.Services;
using Cart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<CartDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddHealthChecks();
builder.Services.AddGrpcHealthChecks()
    .AddCheck("cart-service-1", () => HealthCheckResult.Healthy());
builder.Services.AddConsul(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    CartDbContext db = scope.ServiceProvider.GetRequiredService<CartDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<CartGrpcService>();
app.MapGrpcHealthChecksService();
app.MapGrpcReflectionService();
app.UseConsul();

app.Run();