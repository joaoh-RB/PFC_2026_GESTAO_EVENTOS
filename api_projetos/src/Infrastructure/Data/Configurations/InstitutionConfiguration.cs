using API_Gestao_Eventos.src.Domain;
using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
    {
        public void Configure(EntityTypeBuilder<Institution> builder)
        {
            builder.ToTable("Institutions");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(150);

            builder.HasIndex(i => i.Name).IsUnique();

            builder.Property(i => i.Cnpj)
            .HasConversion(
                cnpj => cnpj == null ? null : cnpj.Number,
                value => value == null ? null : Cnpj.Create(value)
            )
            .HasMaxLength(14);

            builder.HasIndex(i => i.Cnpj).IsUnique().HasFilter("\"Cnpj\" IS NOT NULL");

            builder.Property(i => i.Address).HasMaxLength(250);
            builder.Property(i => i.Phone).HasMaxLength(20);

            builder.HasMany(i => i.Users)
            .WithOne(u => u.Institution)
            .HasForeignKey(u => u.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(i => i.Events)
                .WithOne(e => e.Institution)
                .HasForeignKey(e => e.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
