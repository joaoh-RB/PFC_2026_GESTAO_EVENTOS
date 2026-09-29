using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class LegalDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public LegalDocumentType Type { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<TermAcceptance> Acceptances { get; set; } = new List<TermAcceptance>();
    }
}