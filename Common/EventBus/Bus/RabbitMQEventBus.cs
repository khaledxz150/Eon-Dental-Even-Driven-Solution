using System.Text;
using System.Text.Json;

using EventBus.Abstracts;
using EventBus.Handlers;
using EventBus.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.Bus
{
    public class RabbitMQEventBus : IEventBus, IAsyncDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMQEventBus> _logger;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _exchangeName;
        private readonly Dictionary<string, List<Type>> _eventHandlers = new();
        private readonly List<string> _queueNames = new();

        private RabbitMQEventBus(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQEventBus> logger,
            string exchangeName,
            IConnection connection,
            IChannel channel)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _exchangeName = exchangeName;
            _connection = connection;
            _channel = channel;
        }

        public static async Task<RabbitMQEventBus> CreateAsync(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQEventBus> logger,
            string connectionString,
            string exchangeName)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct);

            return new RabbitMQEventBus(serviceScopeFactory, logger, exchangeName, connection, channel);
        }

        public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
        {
            var eventName = @event.GetType().Name;
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: eventName,
                body: body);

            _logger.LogInformation("Published event {EventName}: {Event}", eventName, message);
        }

        public async Task SubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            if (!_eventHandlers.ContainsKey(eventName))
                _eventHandlers[eventName] = new List<Type>();

            if (_eventHandlers[eventName].Contains(handlerType))
                throw new ArgumentException($"Handler {handlerType.Name} already registered for {eventName}");

            _eventHandlers[eventName].Add(handlerType);

            var queueName = $"{AppDomain.CurrentDomain.FriendlyName}.{eventName}";
            _queueNames.Add(queueName);

            await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(queue: queueName, exchange: _exchangeName, routingKey: eventName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, args) =>
            {
                var message = Encoding.UTF8.GetString(args.Body.ToArray());

                try
                {
                    await ProcessEventAsync(eventName, message);
                    await _channel.BasicAckAsync(args.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {Message}", message);
                    // Optionally Nack
                    await _channel.BasicNackAsync(args.DeliveryTag, false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Subscribed to {EventName} with {HandlerType}", eventName, handlerType.Name);
        }

        public async Task UnsubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);

            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
                return;

            handlers.Remove(handlerType);
            if (!handlers.Any())
            {
                _eventHandlers.Remove(eventName);
                var queueName = _queueNames.FirstOrDefault(q => q.EndsWith(eventName));
                if (queueName != null)
                {
                    await _channel.QueueUnbindAsync(queue: queueName, exchange: _exchangeName, routingKey: eventName);
                    await _channel.QueueDeleteAsync(queueName);
                    _queueNames.Remove(queueName);

                    _logger.LogInformation("Unsubscribed from event {EventName}", eventName);
                }
            }
        }

        private async Task ProcessEventAsync(string eventName, string message)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var handlers)) return;

            using var scope = _serviceScopeFactory.CreateScope();
            var eventType = GetEventTypeByName(eventName);
            var integrationEvent = JsonSerializer.Deserialize(message, eventType);

            foreach (var handlerType in handlers)
            {
                var handler = scope.ServiceProvider.GetService(handlerType);
                if (handler == null) continue;

                var concreteHandler = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                var method = concreteHandler.GetMethod("HandleAsync");

                if (method != null)
                {
                    var task = (Task)method.Invoke(handler, new[] { integrationEvent });
                    await task;
                }
            }
        }

        private static Type GetEventTypeByName(string eventName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == eventName && t.IsSubclassOf(typeof(IntegrationEvent)));
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel.IsOpen)
                await _channel.CloseAsync();

            if (_connection.IsOpen)
                await _connection.CloseAsync();
        }
    }
}
