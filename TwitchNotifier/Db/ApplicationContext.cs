using Microsoft.EntityFrameworkCore;
using TwitchNotifier.Models;

namespace TwitchNotifier.Db
{
    internal class ApplicationContext : DbContext
    {
        public DbSet<Notification> Notifications => Set<Notification>();
        public ApplicationContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=notifications.db");
        }
    }
}
