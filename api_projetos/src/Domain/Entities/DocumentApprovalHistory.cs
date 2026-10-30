namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class DocumentApprovalHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        public Guid ReviewerId { get; set; }
        public User Reviewer { get; set; } = null!;
        public DocumentStatus Status { get; set; }
        public string? Feedback { get; set; }
        public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    }
}
