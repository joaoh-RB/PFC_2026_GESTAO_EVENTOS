using API_Gestao_Eventos.src.Domain.Entities;

namespace API_Gestao_Eventos.src.Application.DTO.EventDocument
{
    public class EventDocumentDto
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DocumentStatus Status { get; set; }
    }
}
