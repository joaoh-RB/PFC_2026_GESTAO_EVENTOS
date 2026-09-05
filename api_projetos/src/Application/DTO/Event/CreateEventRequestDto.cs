using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class CreateEventRequestDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required Guid InstitutionId { get; set; }
        public required DateTimeOffset StartDate { get; set; }
        public required DateTimeOffset EndDate { get; set; }
        public required int Capacity { get; set; }
        public required bool AllowDocuments { get; set; }
        public required EventType EventType { get; set; }
        public required ICollection<Guid> AllowedCourses { get; set; } = new List<Guid>();
    }
}