using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configuration
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.EntityName).HasMaxLength(200).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(100);
            builder.Property(a => a.ChangedProperties).HasMaxLength(1000);
            builder.Property(a => a.OldValues).HasColumnType("jsonb");
            builder.Property(a => a.NewValues).HasColumnType("jsonb");
            builder.Property(a => a.Description).HasMaxLength(2000);
            builder.Property(a => a.Timestamp).IsRequired();

            builder.HasIndex(a => a.UserId);
            builder.HasIndex(a => a.EntityName);
            builder.HasIndex(a => a.Timestamp);
        }
    }
}
