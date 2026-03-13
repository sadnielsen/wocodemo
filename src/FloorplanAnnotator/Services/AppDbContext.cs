using FloorplanAnnotator.Models;
using Microsoft.EntityFrameworkCore;

namespace FloorplanAnnotator.Services
{
    public class AppDbContext : DbContext
    {
        private readonly string _dbPath;

        public AppDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Annotation> Annotations => Set<Annotation>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.FloorplanPath).IsRequired();
                entity.Property(e => e.AnnotationsPath).IsRequired();
                entity.HasMany(e => e.Annotations)
                      .WithOne(a => a.Project)
                      .HasForeignKey(a => a.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Annotation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Label).HasMaxLength(200);
                entity.Property(e => e.Coordinates).IsRequired();
                entity.Property(e => e.Color).HasMaxLength(20);
            });
        }
    }
}
