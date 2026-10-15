using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDayRoute = RaceDay.Api.Models.Route;

namespace RaceDay.Api.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<EventCategory> EventCategories { get; set; } = null!;
        public DbSet<Enrolment> Enrolments { get; set; } = null!;
        public DbSet<Result> Results { get; set; } = null!;
        public DbSet<RaceDayRoute> Routes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Event>().ToTable("Events");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<EventCategory>().ToTable("EventCategories");
            modelBuilder.Entity<Enrolment>().ToTable("Enrolments");
            modelBuilder.Entity<Result>().ToTable("Results");
            modelBuilder.Entity<RaceDayRoute>().ToTable("Routes");
        }
    }
}