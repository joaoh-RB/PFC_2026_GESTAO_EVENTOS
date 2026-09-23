namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class TermAcceptance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public Guid LegalDocumentId { get; set; }
        public LegalDocument LegalDocument { get; set; } = null!;
        public string Version { get; set; } = string.Empty;
        public DateTime AcceptedAtUtc { get; set; } = DateTime.UtcNow;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}