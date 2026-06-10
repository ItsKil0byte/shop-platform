using System.Text;
using System.Text.Json;
using Order.Application.Interfaces;
using Order.Infrastructure.Messaging.Events;
using RabbitMQ.Client;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Публикует события заказов в RabbitMQ.
/// Заменяет ConsoleEventPublisher — поменяй регистрацию в Program.cs.
/// </summary>
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public const string OrderPaidQueue      = "order.paid";
    public const string OrderCancelledQueue = "order.cancelled";

    public RabbitMqEventPublisher(string hostName)
    {
        ConnectionFactory factory = new() { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel    = _connection.CreateModel();

        // Объявляем очереди — идемпотентная операция, безопасно вызывать при каждом старте
        _channel.QueueDeclare(queue: OrderPaidQueue,      durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: OrderCancelledQueue, durable: true, exclusive: false, autoDelete: false);
    }

    public Task PublishOrderPaidEventAsync(Guid orderId, string userId, CancellationToken cancellationToken = default)
    {
        OrderPaidEvent payload = new(orderId.ToString(), userId);
        Publish(OrderPaidQueue, payload);
        return Task.CompletedTask;
    }

    public Task PublishOrderCancelledEventAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        OrderCancelledEvent payload = new(orderId.ToString());
        Publish(OrderCancelledQueue, payload);
        return Task.CompletedTask;
    }

    private void Publish<T>(string queue, T payload)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        IBasicProperties props = _channel.CreateBasicProperties();
        props.Persistent = true; // сообщение переживёт перезапуск RabbitMQ

        _channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: props, body: body);
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}
