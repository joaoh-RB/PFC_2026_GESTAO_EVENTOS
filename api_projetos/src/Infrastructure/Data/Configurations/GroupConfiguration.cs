using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("Groups");

            builder.HasKey(g => g.Id);

            builder.Property(g => g.IsActive)
                   .HasDefaultValue(true);

            builder.Property(g => g.CreatedAt)
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(g => g.Leader)
                   .WithMany()
                   .HasForeignKey(g => g.LeaderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(g => g.Event)
                   .WithMany()
                   .HasForeignKey(g => g.EventId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(g => g.Documents)
                   .WithOne(d => d.Group)
                   .HasForeignKey(d => d.GroupId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}