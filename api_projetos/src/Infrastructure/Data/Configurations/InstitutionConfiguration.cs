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
