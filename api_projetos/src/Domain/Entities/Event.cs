using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public Institution Institution { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<EventStudent> EventStudents { get; set; } = new List<EventStudent>();
        public ICollection<Course> AllowedCourses { get; set; } = new List<Course>();
        public ICollection<EventAllowedCourse> EventAllowedCourses { get; set; } = new List<EventAllowedCourse>();
        public bool IsActive { get; set; } = true;
        public bool AllowDocuments { get; set; }
        public EventType EventType { get; set; }
        //public EventStatus EventStatus { get; set; }
    }
    public enum RegistrationStatus
    {
        Confirmed = 1,
        Canceled = 2,
        Attended = 3
    }
    public enum EventStatus
    {
        Agendado = 1,
        Cancelado = 2
    }

    public class EventStudent
    {
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public RegistrationStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class EventAllowedCourse
    {
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

