using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Teacher : User
    {
        public Teacher()
        {
            Role = UserRole.Professor;
            ApprovalStatus = UserApprovalStatus.Aprovado;
        }
        public bool IsInstitutionAdmin { get; set; } = false;
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
