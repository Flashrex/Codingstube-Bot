using Codingstube.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Codingstube.Database {
    internal class DatabaseContext : DbContext {

        public DbSet<UserEntity> Users { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<UserEntity>(u => {
                u.HasKey(u => u.Id);
                u.Property(u => u.DiscordId).IsRequired();
            });
        }
    }
}
