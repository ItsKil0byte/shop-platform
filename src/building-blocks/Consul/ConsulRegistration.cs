using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Consul;

public static class ConsulRegistration
{
    /// <summary>
    /// Регистрирует сервис в Consul при старте и дерегистрирует при остановке.
    /// Вызывай в Program.cs: app.UseConsul();
    /// 
    /// Требует в appsettings.json:
    /// "Consul": {
    ///   "Host": "http://consul:8500",
    ///   "ServiceName": "payment-service",
    ///   "ServiceId": "payment-service-1",
    ///   "ServiceHost": "payment-api",
    ///   "ServicePort": 8080
    /// }
    /// </summary>
    public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
    {
        IConsulClient consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        IHostApplicationLifetime lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        ILogger logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ConsulRegistration");
        IConfiguration config = app.ApplicationServices.GetRequiredService<IConfiguration>();

        string serviceName = config["Consul:ServiceName"] ?? "unknown-service";
        string serviceId = config["Consul:ServiceId"] ?? $"{serviceName}-{Guid.NewGuid()}";
        string serviceHost = config["Consul:ServiceHost"] ?? "localhost";
        int servicePort = int.Parse(config["Consul:ServicePort"] ?? "8080");
        string consulHost = config["Consul:Host"] ?? "http://consul:8500";
        string? healthCheckPath = config["Consul:HealthCheckPath"];

        AgentServiceRegistration registration = new()
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceHost,
            Port = servicePort
        };

        if (!string.IsNullOrEmpty(healthCheckPath))
        {
            registration.Check = new AgentServiceCheck
            {
                HTTP = $"http://{serviceHost}:{servicePort}{healthCheckPath}",
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            };
        }
        else
        {
            registration.Check = new AgentServiceCheck
            {
                GRPC = $"{serviceHost}:{servicePort}",
                GRPCUseTLS = false,
                Interval = TimeSpan.FromSeconds(10),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
            };
        }

        lifetime.ApplicationStarted.Register(() =>
        {
            consulClient.Agent.ServiceRegister(registration).Wait();
            logger.LogInformation("Сервис {ServiceName} зарегистрирован в Consul (id: {ServiceId})", serviceName, serviceId);
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(serviceId).Wait();
            logger.LogInformation("Сервис {ServiceName} дерегистрирован из Consul", serviceName);
        });

        return app;
    }

    /// <summary>
    /// Добавляет IConsulClient в DI.
    /// Вызывай в Program.cs: builder.Services.AddConsul(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddConsul(this IServiceCollection services, IConfiguration configuration)
    {
        string consulHost = configuration["Consul:Host"] ?? "http://consul:8500";

        services.AddSingleton<IConsulClient>(_ =>
            new ConsulClient(cfg => cfg.Address = new Uri(consulHost)));

        return services;
    }
}
