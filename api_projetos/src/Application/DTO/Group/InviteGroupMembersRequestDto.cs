namespace API_Gestao_Eventos.src.Application.DTO.Group
{
    public class InviteGroupMembersRequestDto
    {
        public required List<Guid> StudentIds { get; set; }
    }
}