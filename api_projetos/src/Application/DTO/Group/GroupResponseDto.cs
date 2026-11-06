namespace API_Gestao_Eventos.src.Application.DTO.Group
{
    public class GroupResponseDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int Number { get; set; }
        public Guid LeaderId { get; set; }
        public string LeaderName { get; set; } = string.Empty;
        public List<GroupMemberDto> Members { get; set; } = new();
    }
}