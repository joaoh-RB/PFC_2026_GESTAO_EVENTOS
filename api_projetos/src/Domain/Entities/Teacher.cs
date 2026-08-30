using API_Gestao_Eventos.src.Domain.Enums;
using System.Data;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Teacher : User
    {
        public Teacher()
        {
            Role = UserRole.Professor;
        }
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
