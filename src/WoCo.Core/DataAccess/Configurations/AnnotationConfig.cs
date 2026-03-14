using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WoCo.Core.Models;

namespace WoCo.Core.DataAccess.Configurations;

public class AnnotationConfig : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.ProjectId);

        builder.HasOne(a => a.Project)
            .WithMany(p => p.Annotations)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}