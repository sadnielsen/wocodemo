using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess.Configurations;

public class AnnotationRevisionConfig : IEntityTypeConfiguration<AnnotationRevision>
{
    public void Configure(EntityTypeBuilder<AnnotationRevision> builder)
    {
        builder.HasKey(ar => ar.Id);

        builder.Property(ar => ar.RevisionNumber).IsRequired();
        builder.Property(ar => ar.Source).HasConversion<string>().IsRequired();
        builder.Property(ar => ar.Label).IsRequired().HasMaxLength(200);
        builder.Property(ar => ar.Description).HasMaxLength(2000);
        builder.Property(ar => ar.Type).HasConversion<string>().IsRequired();
        builder.Property(ar => ar.Color).IsRequired().HasMaxLength(20);

        builder.Property(ar => ar.RawCoordinates)
            .IsRequired()
            .HasConversion(
                v => string.Join(",", v.Select(d => d.ToString("0.######", CultureInfo.InvariantCulture))),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
                    .ToArray())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<double[]>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToArray()));

        builder.Property(ar => ar.IsDeleted).IsRequired();
        builder.Property(ar => ar.CreatedAtUtc).IsRequired();

        builder.HasOne(ar => ar.Annotation)
            .WithMany(a => a.Revisions)
            .HasForeignKey(ar => ar.AnnotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.FloorplanRevision)
            .WithMany(fr => fr.AnnotationRevisions)
            .HasForeignKey(ar => ar.FloorplanRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ar => new { ar.AnnotationId, ar.FloorplanRevisionId })
            .IsUnique();

        builder.HasIndex(ar => new { ar.FloorplanRevisionId, ar.IsDeleted });
        builder.HasIndex(ar => new { ar.AnnotationId, ar.RevisionNumber });
    }
}