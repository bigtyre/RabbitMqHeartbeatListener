using Microsoft.Extensions.Logging;

namespace RabbitMqHeartbeatListener;

public class RabbitMqHeartbeatPublisherService(
    ILogger<RabbitMqHeartbeatPublisherService> logger,
    MessagePublisher messagePublisher,
    RabbitMqHeartbeatPublisherSettings settings
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            var heartbeatInterval = TimeSpan.FromSeconds(settings.HeartbeatSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
               // logger.LogTrace("Publishing heartbeat message");

                await messagePublisher.PublishJsonMessageAsync(
                    "heartbeats",
                    new
                    {
                        settings.AppId,
                        Time = DateTimeOffset.Now.ToString("O")
                    }
                );

                await Task.Delay(heartbeatInterval, stoppingToken);
            }
        }, stoppingToken);
    }
}