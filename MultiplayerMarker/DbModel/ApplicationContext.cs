using Microsoft.EntityFrameworkCore;

namespace MultiplayerMarker.DbModel
{
    public class ApplicationContext : DbContext
    {
        public DbSet<UserActionDbLog> UserActions { get; set; }
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();   // создаем базу данных при первом обращении
        }
    }
}
