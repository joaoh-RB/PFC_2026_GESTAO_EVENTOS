namespace API_Gestao_Eventos.src.Domain.Entities
{
    public class Institution
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
