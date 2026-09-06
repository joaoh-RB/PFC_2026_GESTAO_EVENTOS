using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Administrator : User
    {
        public Administrator()
        {
            Role = UserRole.Administrador;
            ApprovalStatus = UserApprovalStatus.Aprovado;
        }
    }
}
