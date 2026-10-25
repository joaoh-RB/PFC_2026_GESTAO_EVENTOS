using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class AttendanceRegistrationDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public bool Attended { get; set; }
        public DateTime? AttendedAt { get; set; }
        public AttendanceMethod? Method { get; set; }
    }
}