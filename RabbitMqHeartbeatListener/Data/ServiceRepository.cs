using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging;

namespace RabbitMqHeartbeatListener.Data
{
    public class ServiceRepository(IServiceProvider serviceProvider, EventBus eventBus, ILogger<ServiceRepository> logger)
    {
        private static AppDbContext GetContext(IServiceScope scope)
        {
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public void AddService(string appId, string name)
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);

            logger.LogInformation("Adding new service {name}", name);
            var service = new Service(appId, name);
            context.Services.Add(service);

            context.SaveChanges();
            logger.LogInformation("Service added {name}", name);
            eventBus.OnServiceAdded();
        }

        public void UpdateService(string appId, string name)
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);
            var service = context.Services.First(s => s.AppId == appId);
            service.Name = name;

            context.SaveChanges();

            eventBus.OnServiceUpdated();
        }

        public void UpdateServiceHeartbeatTime(string appId, DateTimeOffset? heartbeatTime)
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);
            var service = context.Services.FirstOrDefault(s => s.AppId == appId);

            if (service is null)
            {
                AddService(appId, appId);
                service = context.Services.FirstOrDefault(s => s.AppId == appId);
            }

            if (service is null) throw new KeyNotFoundException($"No service registered for app ID: '{appId}' and adding it was unsuccessful.");

            var newHeartbeatTime = heartbeatTime?.ToUniversalTime().DateTime;

            // Update the service's heartbeat time if the new time is greater
            if (service.LastHeartbeatTime is not null && !(newHeartbeatTime > service.LastHeartbeatTime))
            {
                return;
            }

            service.LastHeartbeatTime = newHeartbeatTime;
            logger.LogTrace("Updated heartbeat for service {appId}.", service.AppId);

            context.SaveChanges();

            eventBus.OnServiceUpdated();
        }

        public List<Service> GetServicesWithIssues(DateTime thresholdTime)
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);
            return [.. context.Services.Where(s => s.LastHeartbeatTime < thresholdTime)];
        }

        public List<Service> GetServices()
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);
            return [.. context.Services];
        }

        public void DeleteService(string appId)
        {
            using var scope = serviceProvider.CreateScope();
            using var context = GetContext(scope);
            var service = context.Services.First(s => s.AppId == appId);

            context.Remove(service);

            context.SaveChanges();

            eventBus.OnServiceDeleted();
        }
    }


    public class ServiceHeartbeatMessage
    {
        public string? AppId { get; set; }
        public DateTimeOffset Time { get; set; }
    }

}
