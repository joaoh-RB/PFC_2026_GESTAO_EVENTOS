using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public abstract class User
    {

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool TwoFactorEnabled { get; set; } = false;

        public string? TwoFactorSecretKey { get; set; }
        public Guid? InstitutionId { get; set; }
        public Institution? Institution { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
