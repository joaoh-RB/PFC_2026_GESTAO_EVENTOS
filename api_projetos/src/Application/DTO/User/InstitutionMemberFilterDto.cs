using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.User
{
    public class InstitutionMemberFilterDto
    {
        public UserRole? UserRole { get; set; }
        public Guid? InstitutionId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }
}
