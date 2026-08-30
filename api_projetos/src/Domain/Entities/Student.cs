using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Student : User
    {
        public Student()
        {
            Role = UserRole.Aluno;
        }
        public string? UniqueIdentifier { get; set; }

        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;
    }
}
