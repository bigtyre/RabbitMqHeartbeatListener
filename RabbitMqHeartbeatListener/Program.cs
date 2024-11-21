using Prometheus;
using Microsoft.EntityFrameworkCore;
using RabbitMqHeartbeatListener;
using RabbitMqHeartbeatListener.Components;
using RabbitMqHeartbeatListener.Data;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables();
config.AddKeyPerFile("/var/run/secrets", optional: true);
#if DEBUG
config.AddUserSecrets<Program>();
#endif

var settings = new AppSettings();
config.Bind(settings);

var rabbitMqSettings = settings.RabbitMq;

var services = builder.Services;

var heartbeatSettings = new RabbitMqHeartbeatPublisherSettings(settings.AppId ?? throw new Exception("App Id not configured."));

services.AddSingleton(heartbeatSettings);
services.AddSingleton(rabbitMqSettings);
services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);
services.AddSingleton<ServiceRepository>();
services.AddSingleton<EventBus>();
services.AddTransient(svc => rabbitMqSettings.CreateConnectionFactory());
services.AddSingleton<RabbitMqConnectionProvider>();
services.AddTransient<MessagePublisher>();
services.AddSingleton(settings);

services.AddHostedService<RabbitMqListenerService>();
services.AddHostedService<RabbitMqHeartbeatPublisherService>();
services.AddHostedService<RabbitMqHeartbeatMetricUpdateService>();

// Add services to the container.
services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();


// Apply database migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var dir = Path.GetDirectoryName(dbContext.DatabasePath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(dbContext.DatabasePath))
        {
            Console.WriteLine("Database file not found.");
            //File.Create(dbContext.DatabasePath);
        }
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var typeName = ex.GetType().Name;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UsePathBase(settings.BasePath);

app.UseHttpsRedirection();

app.UseMetricServer();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
