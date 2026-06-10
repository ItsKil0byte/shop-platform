using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Worker;

public class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly string _hostName;

    private IConnection? _connection;
    private IModel? _channel;

    // Имена очередей — должны совпадать с RabbitMqEventPublisher
    private const string OrderPaidQueue      = "order.paid";
    private const string OrderCancelledQueue = "order.cancelled";

    public NotificationWorker(ILogger<NotificationWorker> logger, IConfiguration configuration)
    {
        _logger   = logger;
        _hostName = configuration["RabbitMq:Host"] ?? "rabbitmq";
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // Retry подключения — RabbitMQ может ещё не быть готов
        int retries = 10;
        while (retries-- > 0)
        {
            try
            {
                ConnectionFactory factory = new() { HostName = _hostName };
                _connection = factory.CreateConnection();
                _channel    = _connection.CreateModel();

                _channel.QueueDeclare(queue: OrderPaidQueue,      durable: true, exclusive: false, autoDelete: false);
                _channel.QueueDeclare(queue: OrderCancelledQueue, durable: true, exclusive: false, autoDelete: false);

                _logger.LogInformation("Notification Worker подключён к RabbitMQ ({Host})", _hostName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ недоступен, повтор через 3с... ({Message})", ex.Message);
                Thread.Sleep(3000);
            }
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("Канал RabbitMQ не инициализирован. Worker остановлен.");
            return Task.CompletedTask;
        }

        // Подписка на OrderPaid
        EventingBasicConsumer paidConsumer = new(_channel);
        paidConsumer.Received += (_, ea) =>
        {
            string json = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("[OrderPaid] Получено: {Json}", json);

            // Здесь можно добавить отправку email / push / SMS
            // Например: emailService.Send(...)

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };
        _channel.BasicConsume(queue: OrderPaidQueue, autoAck: false, consumer: paidConsumer);

        // Подписка на OrderCancelled
        EventingBasicConsumer cancelledConsumer = new(_channel);
        cancelledConsumer.Received += (_, ea) =>
        {
            string json = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("[OrderCancelled] Получено: {Json}", json);

            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        };
        _channel.BasicConsume(queue: OrderCancelledQueue, autoAck: false, consumer: cancelledConsumer);

        // Worker живёт пока не придёт cancellation
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
