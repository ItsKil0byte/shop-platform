using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace BuildingBlocks.Logging;

public static class SerilogExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder, string serviceName)
    {
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName);

        if (builder.Environment.IsDevelopment())
        {
            // Для локального запуска
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) {Message:lj}{NewLine}{Exception}"
            );
        }
        else
        {
            // Для контейнеров
            loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        builder.Host.UseSerilog();

        return builder;
    }
}