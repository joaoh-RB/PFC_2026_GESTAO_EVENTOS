using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.OriginalFileName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(d => d.StorageKey)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(d => d.ContentType)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(d => d.Status)
                   .HasDefaultValue(DocumentStatus.Pending);

            builder.Property(d => d.SubmittedAt)
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(d => d.Group)
                   .WithMany(g => g.Documents)
                   .HasForeignKey(d => d.GroupId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Sender)
                   .WithMany()
                   .HasForeignKey(d => d.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(d => d.ApprovalHistory)
                   .WithOne(a => a.Document)
                   .HasForeignKey(a => a.DocumentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
