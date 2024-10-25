using RabbitMQ.Client;

namespace RabbitMqHeartbeatListener
{
    public class RabbitMqSettings
    {
        public Uri Uri { get; set; }
        public string? ClientProvidedName { get; set; }

        public IConnectionFactory CreateConnectionFactory()
        {
            var factory = new ConnectionFactory()
            {
                Uri = Uri,
                ClientProvidedName = ClientProvidedName,
                AutomaticRecoveryEnabled = true, // Automatically recover connections
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5) // Wait time before retrying connection };
            };

            return factory;
        }
    }
}