using API_Gestao_Eventos.src.Domain.Enums;

namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public AuditOperationType OperationType { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? ChangedProperties { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? Description { get; set; }
    }
}
