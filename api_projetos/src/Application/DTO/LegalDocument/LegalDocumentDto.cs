namespace API_Gestao_Eventos.src.Application.DTO.LegalDocument
{
    public class LegalDocumentDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
    }
}