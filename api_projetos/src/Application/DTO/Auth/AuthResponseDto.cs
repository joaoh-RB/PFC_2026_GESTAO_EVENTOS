namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class AuthResponseDto
    {
        public bool RequiresTwoFactor { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public UserResponseDto? User { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Guid? InstitutionId { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }
}
