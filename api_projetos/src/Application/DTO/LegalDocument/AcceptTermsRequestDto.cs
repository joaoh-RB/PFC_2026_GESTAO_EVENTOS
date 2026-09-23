namespace API_Gestao_Eventos.src.Application.DTO.LegalDocument
{
    public class AcceptTermsRequestDto
    {
        public required List<Guid> DocumentIds { get; set; }
    }
}