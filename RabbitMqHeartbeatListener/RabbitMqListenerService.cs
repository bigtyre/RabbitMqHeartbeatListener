using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqHeartbeatListener.Data;

namespace RabbitMqHeartbeatListener
{
    public class RabbitMqListenerService(
        RabbitMqConnectionProvider rabbitMqConnectionProvider,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqListenerService> logger
    ) : BackgroundService
    {
        private IModel? _channel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _channel = await rabbitMqConnectionProvider.CreateChannelAsync();

                        _channel.ExchangeDeclarePassive(exchange: "default");
                        var queueName = _channel.QueueDeclare().QueueName;
                        _channel.QueueBind(queue: queueName, exchange: "default", routingKey: "heartbeats");

                        logger.LogInformation("Connected to RabbitMQ and listening on the 'heartbeats' topic.");

                        var consumer = new EventingBasicConsumer(_channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var messageText = Encoding.UTF8.GetString(body);

                            ServiceHeartbeatMessage? heartbeatMessage;
                            try
                            {
                                heartbeatMessage = JsonSerializer.Deserialize<ServiceHeartbeatMessage>(messageText);
                                if (heartbeatMessage == null || string.IsNullOrEmpty(heartbeatMessage.AppId))
                                {
                                    throw new Exception("Invalid message format");
                                }
                            }
                            catch
                            {
                                // Log and discard the message if deserialization fails
                                logger.LogTrace("Received invalid heartbeat message. Discarding.");
                                return;
                            }

                            // Use a new scope to get a fresh instance of the DbContext
                            using var scope = serviceProvider.CreateScope();
                            var serviceRepo = scope.ServiceProvider.GetRequiredService<ServiceRepository>();

                            try
                            {
                                var receivedTimeUtc = heartbeatMessage.Time.UtcDateTime;

                                // Get the existing service from the repository
                                serviceRepo.UpdateServiceHeartbeatTime(heartbeatMessage.AppId, receivedTimeUtc);
                            }
                            catch (Exception ex)
                            {
                                // Log the exception (logging framework preferred)
                                logger.LogError(ex, "Error processing heartbeat message: {errorMessage}", ex.Message);
                            }
                        };

                        _channel.BasicConsume(
                            queue: queueName,
                            autoAck: true,
                            consumer: consumer
                        );

                        await Task.Delay(Timeout.Infinite, stoppingToken); // Keep running until cancellation is requested
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in RabbitMQ listener, reconnecting in 5 seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait and retry connection
                    }
                }
            }, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Close();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}