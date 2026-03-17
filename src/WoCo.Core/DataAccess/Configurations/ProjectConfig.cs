using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess.Configurations;

public class ProjectConfig : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => p.CreatedAtUtc);

        // Relationships
        builder.HasMany(p => p.FloorplanRevisions)
            .WithOne(fr => fr.Project)
            .HasForeignKey(fr => fr.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Annotations)
            .WithOne(a => a.Project)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}