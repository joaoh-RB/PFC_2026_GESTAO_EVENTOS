using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class DocumentApprovalHistoryConfiguration : IEntityTypeConfiguration<DocumentApprovalHistory>
    {
        public void Configure(EntityTypeBuilder<DocumentApprovalHistory> builder)
        {
            builder.ToTable("DocumentApprovalHistories");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Status)
                .IsRequired();

            builder.Property(d => d.Feedback)
                .HasMaxLength(1000);

            builder.Property(d => d.ReviewedAt)
                .HasDefaultValueSql("NOW()");

            builder.HasOne(d => d.Document)
                .WithMany(d => d.ApprovalHistory)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Reviewer)
                .WithMany()
                .HasForeignKey(d => d.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => new { d.DocumentId, d.ReviewedAt });
        }
    }
}
