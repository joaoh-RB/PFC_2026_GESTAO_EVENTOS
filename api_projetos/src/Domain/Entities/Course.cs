namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Course
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
