using Microsoft.EntityFrameworkCore;

namespace RabbitMqHeartbeatListener.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Service> Services { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=/data/services.db");
        }
    }
}
