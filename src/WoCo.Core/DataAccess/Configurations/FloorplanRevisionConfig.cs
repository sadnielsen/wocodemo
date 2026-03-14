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
        builder.Property(fr => fr.FloorplanPath).IsRequired();
        builder.Property(fr => fr.SourceCoordinateSystem).HasConversion<int>().IsRequired();
        builder.Property(fr => fr.SourceOrigin).HasConversion<int>().IsRequired();
        builder.Property(fr => fr.SourceWidth).IsRequired();
        builder.Property(fr => fr.SourceHeight).IsRequired();
        builder.Property(fr => fr.CreatedAtUtc).IsRequired();

        builder.HasIndex(fr => new { fr.ProjectId, fr.RevisionNumber })
            .IsUnique();

        builder.HasOne(fr => fr.Project)
            .WithMany(p => p.FloorplanRevisions)
            .HasForeignKey(fr => fr.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}