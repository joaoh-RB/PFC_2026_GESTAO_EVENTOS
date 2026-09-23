using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
    {
        public void Configure(EntityTypeBuilder<LegalDocument> builder)
        {
            builder.ToTable("LegalDocuments");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Version)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(d => d.Content)
                .IsRequired();

            builder.HasIndex(d => new { d.Type, d.Version }).IsUnique();
        }
    }
}