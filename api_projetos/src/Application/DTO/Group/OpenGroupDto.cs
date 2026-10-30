namespace API_Gestao_Eventos.src.Application.DTO.Group
{
    public class OpenGroupDto
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public string LeaderName { get; set; } = string.Empty;
        public int MembersCount { get; set; }
        public int? MaxSize { get; set; }
        public int? AvailableSlots { get; set; }
        public bool HasPendingRequest { get; set; }
    }
}