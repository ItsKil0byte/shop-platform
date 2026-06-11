using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<Catalog.Application.Interfaces.ICatalogRepository, Catalog.Infrastructure.Persistence.CatalogRepository>();
builder.Services.AddScoped<Catalog.Application.Interfaces.IProductService, Catalog.Application.Services.ProductService>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    CatalogDbContext db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();