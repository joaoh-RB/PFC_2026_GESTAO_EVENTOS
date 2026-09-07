namespace API_Gestao_Eventos.src.Application.DTO.Institution
{
    public class UpdateInstitutionDto
    {
        public required string Name { get; set; }
        public string? Cnpj { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
