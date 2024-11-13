namespace RabbitMqHeartbeatListener
{
    public class AppSettings
    {
        public RabbitMqSettings RabbitMq { get; set; } = new();
        public string AppId { get; set; } = "Rabbit MQ Heartbeat UI";
        public string BasePath { get; set; } = "/";
    }
}
