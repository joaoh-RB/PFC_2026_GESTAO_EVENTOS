using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class TermAcceptanceConfiguration : IEntityTypeConfiguration<TermAcceptance>
    {
        public void Configure(EntityTypeBuilder<TermAcceptance> builder)
        {
            builder.ToTable("TermAcceptances");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Version)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(t => t.IpAddress)
                .IsRequired()
                .HasMaxLength(45);

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.LegalDocument)
                .WithMany(d => d.Acceptances)
                .HasForeignKey(t => t.LegalDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => new { t.UserId, t.LegalDocumentId }).IsUnique();
        }
    }
}