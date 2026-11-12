namespace API_Gestao_Eventos.src.Application.DTO.User
{
    public class InstitutionMemberResponseDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string UserRole { get; set; }
        public required int UserRoleId { get; set; }
        public required Guid InstitutionId { get; set; }
        public required string InstitutionName { get; set; }
        public required bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public IEnumerable<Guid>? CoursesIds { get; set; }
        public IEnumerable<string>? CoursesNames { get; set; }
    }
}
