﻿using Codingstube.Database.Models;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Codingstube.Database {
    internal class DatabaseContext : DbContext {

        public DbSet<User> Users { get; set; }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<User>(u => {
                u.HasKey(u => u.Id);
                u.Property(u => u.DiscordId).IsRequired();
            });
        }
    }
}
