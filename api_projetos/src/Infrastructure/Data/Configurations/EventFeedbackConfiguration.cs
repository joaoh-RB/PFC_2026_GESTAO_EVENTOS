using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class EventFeedbackConfiguration : IEntityTypeConfiguration<EventFeedback>
    {
        public void Configure(EntityTypeBuilder<EventFeedback> builder)
        {
            builder.ToTable("EventFeedbacks", t =>
            {
                t.HasCheckConstraint("CK_EventFeedback_Rating", "\"Rating\" >= 0 AND \"Rating\" <= 10");
            });

            builder.HasKey(ef => ef.Id);

            builder.Property(ef => ef.FeedbackText)
                   .IsRequired(false)
                   .HasMaxLength(1000);

            builder.Property(ef => ef.Rating)
                   .IsRequired();

            builder.Property(ef => ef.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(ef => ef.Event)
                   .WithMany()
                   .HasForeignKey(ef => ef.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ef => ef.Student)
                   .WithMany()
                   .HasForeignKey(ef => ef.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(ef => new { ef.EventId, ef.StudentId })
                   .IsUnique();
        }
    }
}

