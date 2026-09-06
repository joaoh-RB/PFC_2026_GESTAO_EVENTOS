using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.ToTable("Courses");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

            builder.HasOne(c => c.Institution)
            .WithMany()
            .HasForeignKey(c => c.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.Name, c.InstitutionId }).IsUnique();

            builder.HasMany(c => (c.Teachers))
                .WithMany(s => s.Courses)
                .UsingEntity<Dictionary<string, object>>(
                "TeacherCourses",
                j => j.HasOne<Teacher>().WithMany().HasForeignKey("TeacherId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Course>().WithMany().HasForeignKey("CourseId").OnDelete(DeleteBehavior.Cascade)
            );
        }
    }
}
