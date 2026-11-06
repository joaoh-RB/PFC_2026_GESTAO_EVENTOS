namespace API_Gestao_Eventos.src.Application.DTO.Group
{
    public class CreateGroupRequestDto
    {
        public required Guid EventId { get; set; }
        public List<Guid> StudentIds { get; set; } = new();
    }
}