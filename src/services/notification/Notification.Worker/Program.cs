using Notification.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<NotificationWorker>();
    })
    .Build();

host.Run();
