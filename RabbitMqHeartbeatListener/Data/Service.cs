using System.ComponentModel.DataAnnotations;

namespace RabbitMqHeartbeatListener.Data
{
    public class Service(string appId, string name)
    {
        [Key]
        public string AppId { get; set; } = appId;
        public string Name { get; set; } = name;
        public DateTime? LastHeartbeatTime { get; set; }
    }

}
