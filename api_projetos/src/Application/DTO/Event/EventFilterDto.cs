namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class EventFilterDto
    {
        public DateTime? FromDate { get; set; }
        public Guid? InstitutionId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }
}
