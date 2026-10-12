namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid EventId { get; set; }
        public Event? Event { get; set; }
        public required Guid LeaderId { get; set; }
        public Student? Leader { get; set; }
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class GroupMember
    {
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
