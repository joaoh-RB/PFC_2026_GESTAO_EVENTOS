using API_Gestao_Eventos.src.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class RegisterRequestDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public UserRole Role { get; set; } = UserRole.Aluno;
        public required Guid InstitutionId { get; set; }
        public required Guid CourseId { get; set; }
        public required string UniqueIdentifier { get; set; }
    }
}
