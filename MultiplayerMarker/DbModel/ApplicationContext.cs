namespace MultiplayerMarker.DbModel
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// DbContext EF
    /// </summary>
    public class ApplicationContext : DbContext
    {
        public DbSet<UserActionDbLog> UserActions { get; set; }
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
