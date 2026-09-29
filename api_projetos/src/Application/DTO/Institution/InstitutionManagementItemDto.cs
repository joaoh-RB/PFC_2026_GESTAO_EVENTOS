namespace API_Gestao_Eventos.src.Application.DTO.Institution
{
    public class InstitutionManagementItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}