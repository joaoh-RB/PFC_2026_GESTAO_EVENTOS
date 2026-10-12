using API_Gestao_Eventos.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.ToTable("GroupMembers");

            builder.HasKey(gm => new { gm.GroupId, gm.StudentId });

            builder.HasIndex(gm => new { gm.StudentId, gm.EventId })
                   .IsUnique();

            builder.Property(gm => gm.JoinedAt)
                   .HasDefaultValueSql("NOW()");

            builder.HasOne(gm => gm.Group)
                   .WithMany(g => g.Members)
                   .HasForeignKey(gm => gm.GroupId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(gm => gm.Student)
                   .WithMany()
                   .HasForeignKey(gm => gm.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(gm => gm.Event)
                   .WithMany()
                   .HasForeignKey(gm => gm.EventId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}