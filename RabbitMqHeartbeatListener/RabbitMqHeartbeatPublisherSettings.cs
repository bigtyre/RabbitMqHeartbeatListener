namespace RabbitMqHeartbeatListener
{
    public record RabbitMqHeartbeatPublisherSettings(string AppId, uint HeartbeatSeconds = 2);
}
