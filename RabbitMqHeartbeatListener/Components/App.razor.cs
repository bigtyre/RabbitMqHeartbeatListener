using Microsoft.AspNetCore.Components;

namespace RabbitMqHeartbeatListener.Components
{
    public partial class App : ComponentBase
    {
        [Inject] public AppSettings Settings { get; set; }
    }
}
