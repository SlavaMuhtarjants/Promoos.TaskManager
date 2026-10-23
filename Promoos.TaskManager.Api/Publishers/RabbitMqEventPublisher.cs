using Microsoft.Extensions.Options;

using Promoos.TaskManager.Api.Configuration;
using Promoos.TaskManager.Api.Contracts;

using RabbitMQ.Client;

using System.Text.Json;

namespace Promoos.TaskManager.Api.Publishers;

public sealed class RabbitMqEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqEventPublisher> logger) : ITaskEventPublisher, IAsyncDisposable
{
    // Max time to wait for an in-flight publication and for the graceful close on shutdown.
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishCompletedAsync(TaskCompletedEvent message, CancellationToken cancellationToken = default)
    {
        const string routingKey = "task.completed";
        var configuration = options.Value;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration.HostName,
                Port = configuration.Port,
                UserName = configuration.UserName,
                Password = configuration.Password
            };

            _connection ??= await factory.CreateConnectionAsync(cancellationToken);

            _channel ??= await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange: configuration.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                cancellationToken: cancellationToken);

            var json = JsonSerializer.SerializeToUtf8Bytes(
                message,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            await _channel.BasicPublishAsync(
                exchange: configuration.Exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                },
                body: json,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish {RoutingKey} for {TaskId}",
                routingKey,
                message.TaskId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // A publication that has already started holds the gate; let it finish before closing anything.
        var acquired = await _gate.WaitAsync(ShutdownTimeout);

        if (!acquired)
        {
            logger.LogWarning("Timed out waiting for an in-flight publication; closing RabbitMQ anyway");
        }

        try
        {
            using var cts = new CancellationTokenSource(ShutdownTimeout);

            if (_channel is not null)
            {
                await _channel.CloseAsync(cts.Token);
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync(cts.Token);
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to close RabbitMQ channel/connection gracefully");
        }
        finally
        {
            if (acquired)
            {
                _gate.Dispose();
            }
        }
    }
}