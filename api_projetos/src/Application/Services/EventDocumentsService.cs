using API_Gestao_Eventos.src.Application.DTO.EventDocument;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Storage;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventDocumentsService(IGroupRepository groupRepository, IStorageService storageService, IDocumentRepository documentRepository)
    {
        private const long MaxFileSizeBytes = 15 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".zip", ".png", ".jpg", ".jpeg"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/zip",
            "application/x-zip-compressed",
            "image/png",
            "image/jpeg"
        };
        public async Task<List<EventDocumentDto>> GetEventDocumentsAsync(Guid eventId, Guid userId)
        {
            var group = await groupRepository.GetGroupForStudentInEventAsync(userId, eventId);
            if (group == null) return new List<EventDocumentDto>();
            return group.Documents.Select(MapToDto).ToList();
        }
        public async Task<EventDocumentDto> AddEventDocumentAsync(Guid eventId, Guid userId, IFormFile file)
        {
            var group = await groupRepository.GetGroupForStudentInEventAsync(userId, eventId);
            if (group == null) group = await groupRepository.AddAsync(new Domain.Entities.Group
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                LeaderId = userId,
                Members = new List<Domain.Entities.GroupMember>
                {
                    new Domain.Entities.GroupMember
                    {
                        StudentId = userId,
                        EventId=eventId
                    }
                },
            });
            if (file == null || file.Length == 0)
                throw new ArgumentException("Nenhum arquivo enviado.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException($"O tamanho do arquivo excede o limite máximo permitido de {MaxFileSizeBytes / (1024 * 1024)} MB.");
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension) || !AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException("Formato de arquivo não suportado.");

            string storageKey;
            using (var stream = file.OpenReadStream())
            {
                var uniqueFileName = $"groups/{group.Id}/{Guid.NewGuid()}{extension}";
                storageKey = await storageService.UploadFileAsync(stream, uniqueFileName, file.ContentType);
            }
            var document = new Document
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                SenderId = userId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StorageKey = storageKey,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                Status = DocumentStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };
            await documentRepository.AddAsync(document);
            return MapToDto(document);
        }
        private static EventDocumentDto MapToDto(Document d)
        {
            return new EventDocumentDto()
            {
                Id = d.Id,
                GroupId = d.GroupId,
                SenderId = d.SenderId,
                SenderName = d.Sender?.Name ?? "Desconhecido",
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                SubmittedAt = d.SubmittedAt,
                Status = d.Status,

            };
        }
    }
}
