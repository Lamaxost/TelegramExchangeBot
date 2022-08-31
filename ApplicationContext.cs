using Microsoft.EntityFrameworkCore;
using TelegramExchangeDB.Models;

namespace TelegramExchangeDB
{
    internal class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            //Database.Migrate();   
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Received>().HasKey("Id");
            modelBuilder.Entity<Sended>().HasKey("Id");
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<VideoProduct> VideoProducts => Set<VideoProduct>();
        public DbSet<Received> ReceivedVideos=> Set<Received>();
        public DbSet<Sended> Sended=> Set<Sended>();
        public DbSet<Ban> BanList => Set<Ban>();
    }
}
