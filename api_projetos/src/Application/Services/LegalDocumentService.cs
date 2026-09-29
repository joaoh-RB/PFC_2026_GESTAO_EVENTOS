using API_Gestao_Eventos.src.Application.DTO.LegalDocument;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class LegalDocumentService(
        LegalDocumentRepository legalDocumentRepository,
        TermAcceptanceRepository termAcceptanceRepository)
    {
        private static LegalDocumentDto ToDto(Domain.Entities.LegalDocument d) => new()
        {
            Id = d.Id,
            Type = d.Type.ToString(),
            Version = d.Version,
            Content = d.Content,
            EffectiveDate = d.EffectiveDate
        };

        public async Task<IEnumerable<LegalDocumentDto>> GetPendingDocumentsAsync(Guid userId)
        {
            var pending = new List<LegalDocumentDto>();

            foreach (var type in Enum.GetValues<LegalDocumentType>())
            {
                var activeDocument = await legalDocumentRepository.GetActiveByTypeAsync(type);
                if (activeDocument == null) continue;

                var alreadyAccepted = await termAcceptanceRepository.HasAcceptedAsync(userId, activeDocument.Id);
                if (!alreadyAccepted)
                {
                    pending.Add(ToDto(activeDocument));
                }
            }

            return pending;
        }

        public async Task<IEnumerable<LegalDocumentDto>> GetCurrentDocumentsAsync()
        {
            var documents = await legalDocumentRepository.GetAllActiveAsync();
            return documents.Select(ToDto);
        }

        public async Task AcceptAsync(Guid userId, List<Guid> documentIds, string ipAddress)
        {
            foreach (var documentId in documentIds)
            {
                var document = await legalDocumentRepository.GetByIdAsync(documentId)
                    ?? throw new KeyNotFoundException("Documento não encontrado.");

                if (!document.IsActive)
                    throw new InvalidOperationException("Esta versão do documento não está mais vigente.");

                var alreadyAccepted = await termAcceptanceRepository.HasAcceptedAsync(userId, documentId);
                if (alreadyAccepted) continue;

                var acceptance = new Domain.Entities.TermAcceptance
                {
                    UserId = userId,
                    LegalDocumentId = document.Id,
                    Version = document.Version,
                    IpAddress = ipAddress
                };
                await termAcceptanceRepository.AddAsync(acceptance);
            }
        }
    }
}