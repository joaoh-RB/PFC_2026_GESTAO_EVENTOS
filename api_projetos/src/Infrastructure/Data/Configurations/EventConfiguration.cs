using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Events");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name)
                   .IsRequired(true)
                   .HasMaxLength(150);
            builder.Property(e => e.Description)
                    .IsRequired(false)
                    .HasMaxLength(300);
            builder.Property(e => e.StartDate)
                    .IsRequired(true);
            builder.Property(e => e.EndDate).IsRequired(true);
            builder.Property(e => e.Capacity)
                   .IsRequired(true);
            builder.HasOne(e => e.Institution)
                   .WithMany(e => e.Events)
                   .HasForeignKey(i => i.InstitutionId)
                   .OnDelete(DeleteBehavior.Restrict);

            //builder.Property(e => e.EventStatus)
            //       .HasDefaultValue(EventStatus.Agendado)
            //       .IsRequired(true);

            builder.HasMany(e => e.Students)
                   .WithMany(s => s.Events)
                   .UsingEntity<EventStudent>(
                        j => j.HasOne(es => es.Student)
                                .WithMany()
                                .HasForeignKey(es => es.StudentId)
                                .OnDelete(DeleteBehavior.Cascade),
                        j => j.HasOne(es => es.Event)
                                .WithMany(es => es.EventStudents)
                                .HasForeignKey(es => es.EventId)
                                .OnDelete(DeleteBehavior.Cascade),
                        j =>
                        {
                            j.ToTable("EventStudents");
                            j.HasKey(es => new { es.EventId, es.StudentId });
                            j.HasQueryFilter(es => es.IsActive);
                        }
                    );

            builder.HasMany(e => e.AllowedCourses)
                .WithMany(c => c.Events)
                .UsingEntity<EventAllowedCourse>(
                    j => j.HasOne(eac => eac.Course)
                          .WithMany()
                          .HasForeignKey(eac => eac.CourseId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne(eac => eac.Event)
                          .WithMany(eac => eac.EventAllowedCourses)
                          .HasForeignKey(eac => eac.EventId)
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable("EventAllowedCourses");
                        j.HasKey(eac => new { eac.EventId, eac.CourseId });
                        j.HasQueryFilter(eac => eac.IsActive);
                    }
                );
        }
    }
}
