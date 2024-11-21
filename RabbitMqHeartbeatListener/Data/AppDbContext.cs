using Microsoft.EntityFrameworkCore;

namespace RabbitMqHeartbeatListener.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Service> Services => Set<Service>();

        public string DatabasePath = "/data/services.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }
    }
}
