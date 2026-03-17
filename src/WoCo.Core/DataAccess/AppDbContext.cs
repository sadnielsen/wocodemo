using Microsoft.EntityFrameworkCore;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<FloorplanRevision> FloorplanRevisions => Set<FloorplanRevision>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<AnnotationRevision> AnnotationRevisions => Set<AnnotationRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}