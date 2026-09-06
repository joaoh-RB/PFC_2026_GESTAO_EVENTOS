namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class EventResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Capacity { get; set; }
        public int ConfirmedRegistrations { get; set; }
        public int EventType { get; set; }
        public bool AllowDocuments { get; set; }
        public List<string> AllowedCourseNames { get; set; } = new();
        public required List<Guid> AllowedCourseIds { get; set; }
    }
}
