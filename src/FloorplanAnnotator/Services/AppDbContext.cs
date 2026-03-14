using FloorplanAnnotator.Models;
using Microsoft.EntityFrameworkCore;

namespace FloorplanAnnotator.Services;

public class AppDbContext : DbContext
{
    private readonly string _dbPath;

    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<FloorplanRevision> FloorplanRevisions => Set<FloorplanRevision>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<AnnotationRevision> AnnotationRevisions => Set<AnnotationRevision>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasMany(e => e.FloorplanRevisions)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Annotations)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FloorplanRevision>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RevisionNumber).IsRequired();
            entity.Property(e => e.FloorplanPath).IsRequired();
            entity.Property(e => e.SourceCoordinateSystem).HasConversion<int>().IsRequired();
            entity.Property(e => e.SourceOrigin).HasConversion<int>().IsRequired();
            entity.Property(e => e.SourceWidth).IsRequired();
            entity.Property(e => e.SourceHeight).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasIndex(e => new { e.ProjectId, e.RevisionNumber })
                .IsUnique();
        });

        modelBuilder.Entity<Annotation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PublicId).IsRequired();
            entity.Property(e => e.ExternalSourceId).HasMaxLength(200);

            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => new { e.ProjectId, e.ExternalSourceId });
        });

        modelBuilder.Entity<AnnotationRevision>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RevisionNumber).IsRequired();
            entity.Property(e => e.Source).HasConversion<int>().IsRequired();
            entity.Property(e => e.Label).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Type).HasConversion<int>().IsRequired();
            entity.Property(e => e.Color).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RawCoordinates).IsRequired();
            entity.Property(e => e.NormalizedCoordinates).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasOne(e => e.Annotation)
                .WithMany(e => e.Revisions)
                .HasForeignKey(e => e.AnnotationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.FloorplanRevision)
                .WithMany(e => e.AnnotationRevisions)
                .HasForeignKey(e => e.FloorplanRevisionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.AnnotationId, e.FloorplanRevisionId })
                .IsUnique();

            entity.HasIndex(e => new { e.FloorplanRevisionId, e.IsDeleted });
            entity.HasIndex(e => new { e.AnnotationId, e.RevisionNumber });
        });
    }
}