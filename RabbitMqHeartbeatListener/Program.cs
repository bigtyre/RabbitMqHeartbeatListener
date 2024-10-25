using RabbitMQ.Client;
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

services.AddHostedService<RabbitMqListenerService>();
services.AddHostedService<RabbitMqHeartbeatPublisherService>();

// Add services to the container.
services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
