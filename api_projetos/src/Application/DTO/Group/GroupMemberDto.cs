using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Application.DTO.Group
{
    public class GroupMemberDto
    {
        public Guid StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public GroupMemberStatus Status { get; set; }
        public bool IsLeader { get; set; }
    }
}