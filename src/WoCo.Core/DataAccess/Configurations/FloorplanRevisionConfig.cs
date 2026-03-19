using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess.Configurations;

public class FloorplanRevisionConfig : IEntityTypeConfiguration<FloorplanRevision>
{
    public void Configure(EntityTypeBuilder<FloorplanRevision> builder)
    {
        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.RevisionNumber).IsRequired();
        builder.Property(fr => fr.FileContent).IsRequired();
        builder.Property(fr => fr.FileType).IsRequired();
        builder.Property(fr => fr.FileName).IsRequired();
        builder.Property(fr => fr.CoordinateSystem).HasConversion<string>().IsRequired();
        builder.Property(fr => fr.Origin).HasConversion<string>().IsRequired();
        builder.Property(fr => fr.Width).IsRequired();
        builder.Property(fr => fr.Height).IsRequired();
        builder.Property(fr => fr.CreatedAtUtc).IsRequired();

        // Transformation properties (relative to initial revision)
        builder.Property(fr => fr.ScaleDenominator).IsRequired().HasDefaultValue(1.0);
        builder.Property(fr => fr.OffsetX).IsRequired().HasDefaultValue(0.0);
        builder.Property(fr => fr.OffsetY).IsRequired().HasDefaultValue(0.0);

        builder.HasIndex(fr => new { fr.ProjectId, fr.RevisionNumber })
            .IsUnique();

        builder.HasOne(fr => fr.Project)
            .WithMany(p => p.FloorplanRevisions)
            .HasForeignKey(fr => fr.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}