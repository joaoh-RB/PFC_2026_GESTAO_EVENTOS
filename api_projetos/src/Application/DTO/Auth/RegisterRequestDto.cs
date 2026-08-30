using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class RegisterRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Aluno;
        public Guid InstitutionId { get; set; }
        public Guid CourseId { get; set; }
        public string? RegistrationNumber { get; set; }
    }
}
