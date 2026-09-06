using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasOne(u => u.Institution)
                .WithMany(i => i.Users)
                .IsRequired(false)
                .HasForeignKey(u => u.InstitutionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.ApprovalStatus)
                .IsRequired()
                .HasDefaultValue(UserApprovalStatus.Pendente);

            builder.HasOne(u => u.ApprovedByUser)
                .WithMany()
                .IsRequired(false)
                .HasForeignKey(u => u.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasDiscriminator<UserRole>("Role")
                .HasValue<Student>(UserRole.Aluno)
                .HasValue<Teacher>(UserRole.Professor)
                .HasValue<Administrator>(UserRole.Administrador);
        }
    }

    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.Property(s => s.UniqueIdentifier)
                .HasMaxLength(20).IsRequired(true);

            builder.HasIndex(s => new { s.UniqueIdentifier, s.InstitutionId })
                .IsUnique()
                .HasFilter("\"UniqueIdentifier\" IS NOT NULL AND \"InstitutionId\" IS NOT NULL");

            builder.HasOne(s => s.Course)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
