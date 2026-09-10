using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class AcademicDepartment : User
    {
        public AcademicDepartment()
        {
            Role = UserRole.Secretaria;
            ApprovalStatus = UserApprovalStatus.Aprovado;
        }
    }
}
