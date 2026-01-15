using LevelSystem_WebServer.Models;
using Microsoft.EntityFrameworkCore;

namespace LevelSystem_WebServer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<PlayerAchievement> PlayerAchievements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PlayerAchievement>()
                .HasKey(pa => new { pa.PlayerId, pa.AchievementId });

            modelBuilder.Entity<PlayerAchievement>()
                .HasOne<Player>()
                .WithMany()
                .HasForeignKey(pa => pa.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerAchievement>()
                .HasIndex(pa => pa.PlayerId);
        }
    }
}