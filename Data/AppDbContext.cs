using Microsoft.EntityFrameworkCore;

namespace OpenIDApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "guest";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
