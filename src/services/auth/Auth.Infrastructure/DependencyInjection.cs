using Auth.Application.Interfaces;
using Auth.Infrastructure.Persistence;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtOptions>(options =>
        {
            IConfigurationSection section = configuration.GetSection("Jwt");
            options.Secret = section["Secret"] ?? string.Empty;
            options.Issuer = section["Issuer"] ?? options.Issuer;
            options.Audience = section["Audience"] ?? options.Audience;

            if (int.TryParse(section["ExpiresDays"], out int expiresDays))
            {
                options.ExpiresDays = expiresDays;
            }
        });
        services.PostConfigure<JwtOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Secret))
            {
                options.Secret = configuration["JWT_SECRET"] ?? string.Empty;
            }
        });
        services.AddScoped<IUserRepository, EFUserRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IPasswordHasher<Auth.Domain.Entities.UserEntity>, PasswordHasher<Auth.Domain.Entities.UserEntity>>();

        return services;
    }
}
