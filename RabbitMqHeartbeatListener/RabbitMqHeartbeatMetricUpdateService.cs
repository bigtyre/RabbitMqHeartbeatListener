using Prometheus;
using RabbitMqHeartbeatListener.Data;

namespace RabbitMqHeartbeatListener;

public class RabbitMqHeartbeatMetricUpdateService(
    ServiceRepository serviceRepo
) : BackgroundService
{
    private static readonly Gauge ServiceHeartbeatAge = Metrics
    .CreateGauge("service_last_heartbeat_age_seconds", "Seconds since the last heartbeat for a service",
        new GaugeConfiguration { LabelNames = ["service"] });


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            var heartbeatInterval = TimeSpan.FromSeconds(5);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now.ToUniversalTime().DateTime;
                var services = serviceRepo.GetServices();
                foreach (var service in services)
                {
                    var serviceName = service.Name;
                    var lastHeartbeatTime = service.LastHeartbeatTime;

                    var metric = ServiceHeartbeatAge.WithLabels(serviceName);
                    if (lastHeartbeatTime is null)
                    {
                        metric.Set(-1);
                        continue;
                    }

                    var secondsSinceLastHeartbeat = (now - lastHeartbeatTime.Value).TotalSeconds;
                    metric.Set(secondsSinceLastHeartbeat);
                }

                await Task.Delay(heartbeatInterval, stoppingToken);
            }
        }, stoppingToken);
    }
}

