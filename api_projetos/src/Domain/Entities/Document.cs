namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid GroupId { get; set; }
        public Group? Group { get; set; }
        public required Guid SenderId { get; set; }
        public User? Sender { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    }
    public enum DocumentStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
