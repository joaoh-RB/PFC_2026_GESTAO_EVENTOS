namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class EventRegistrationDto
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }

    public class EventRegistrationFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
