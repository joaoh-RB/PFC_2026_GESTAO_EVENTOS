using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.User
{
    public class CreateInstitutionMemberRequestDto
    {
        public required string Name { get; set; }
        public required Guid InstitutionId { get; set; }
        public required UserRole UserRole { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public ICollection<Guid>? Courses { get; set; }
    }
    public class UpdateInstitutionMemberRequestDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Password { get; set; }
        public ICollection<Guid>? Courses { get; set; }
    }
}
