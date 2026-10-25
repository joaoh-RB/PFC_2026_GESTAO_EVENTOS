using API_Gestao_Eventos.src.Application.DTO.EventDocument;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Storage;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventDocumentsService(
        IGroupRepository groupRepository,
        IStorageService storageService,
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        IEventRepository eventRepository)
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

        public async Task<List<EventDocumentDto>> GetPendingDocumentsForApprovalAsync(Guid eventId, Guid userId)
        {
            await EnsureCanManageEventDocumentsAsync(eventId, userId);
            var documents = await documentRepository.GetPendingByEventAsync(eventId);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<List<EventDocumentDto>> GetApprovedDocumentsByEventAsync(Guid eventId, Guid userId)
        {
            await EnsureCanManageEventDocumentsAsync(eventId, userId);
            var documents = await documentRepository.GetApprovedByEventAsync(eventId);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<List<EventDocumentDto>> GetReviewedDocumentsByEventAsync(Guid eventId, Guid userId)
        {
            await EnsureCanManageEventDocumentsAsync(eventId, userId);
            var documents = await documentRepository.GetReviewedByEventAsync(eventId);
            return documents.Select(MapToDto).ToList();
        }

        public async Task<EventDocumentDto> ReviewDocumentAsync(Guid documentId, Guid reviewerId, bool approve, string? feedback)
        {
            var document = await documentRepository.GetByIdAsync(documentId)
                ?? throw new KeyNotFoundException("Documento não encontrado.");

            await EnsureCanManageDocumentAsync(document, reviewerId);

            var reviewStatus = approve ? DocumentStatus.Approved : DocumentStatus.Rejected;
            var normalizedFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();

            document.Status = reviewStatus;
            await documentRepository.UpdateAsync(document);

            var approvalRecord = new DocumentApprovalHistory
            {
                DocumentId = document.Id,
                ReviewerId = reviewerId,
                Status = reviewStatus,
                Feedback = normalizedFeedback,
                ReviewedAt = DateTime.UtcNow
            };

            await documentRepository.AddApprovalAsync(approvalRecord);
            document.ApprovalHistory.Add(approvalRecord);

            return MapToDto(document);
        }

        public async Task<EventDocumentDto> AddEventDocumentAsync(Guid eventId, Guid userId, IFormFile file)
        {
            var group = await groupRepository.GetGroupForStudentInEventAsync(userId, eventId)
                ?? throw new InvalidOperationException("Você não faz parte de nenhum grupo neste evento.");
            if (!group.Event!.AllowDocuments)
                throw new InvalidOperationException("Este evento não permite envio de documentos.");
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

        public async Task<(Stream FileStream, string ContentType, string FileName)> DownloadDocumentAsync(Guid documentId, Guid userId)
        {
            var document = await documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException("Documento não encontrado.");
            if (!await CanViewAsync(document, userId))
                throw new UnauthorizedAccessException("Você não tem permissão para acessar este documento.");

            var fileStream = await storageService.GetFileStreamAsync(document.StorageKey);

            return (fileStream, document.ContentType, document.OriginalFileName);
        }

        public async Task DeleteDocumentAsync(Guid documentId, Guid userId)
        {
            var document = await documentRepository.GetByIdAsync(documentId) ?? throw new KeyNotFoundException("Documento não encontrado.");
            if (document.SenderId != userId && document.Group!.LeaderId != userId)
                throw new UnauthorizedAccessException("Somente quem enviou o documento ou o líder do grupo pode excluí-lo.");
            await storageService.DeleteFileAsync(document.StorageKey);
            await documentRepository.DeleteAsync(document);
        }

        private async Task EnsureCanManageEventDocumentsAsync(Guid eventId, Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

            if (user is Student)
                throw new UnauthorizedAccessException("Você não tem permissão para aprovar documentos.");

            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            if (user is Teacher teacher)
            {
                var canAccess = evt.AllowedCourses.Any(c => teacher.Courses.Any(tc => tc.Id == c.Id));
                if (!canAccess)
                    throw new UnauthorizedAccessException("Este evento não pertence aos seus cursos.");
            }
        }

        private async Task EnsureCanManageDocumentAsync(Document document, Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new UnauthorizedAccessException("Usuário não encontrado.");

            if (user is Student)
                throw new UnauthorizedAccessException("Você não tem permissão para aprovar documentos.");

            if (user is Teacher teacher)
            {
                var canAccess = document.Group?.Event?.AllowedCourses.Any(c => teacher.Courses.Any(tc => tc.Id == c.Id)) == true;
                if (!canAccess)
                    throw new UnauthorizedAccessException("Este documento não pertence aos seus cursos.");
            }
        }

        private async Task<bool> CanViewAsync(Document document, Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user is Student)
                return document.Group!.Members.Any(m => m.StudentId == userId && m.Status == GroupMemberStatus.Accepted);
            if (user is Teacher teacher)
                return document.Group!.Event!.AllowedCourses.Any(c => teacher.Courses.Any(tc => tc.Id == c.Id));
            return user != null;
        }

        private static EventDocumentDto MapToDto(Document d)
        {
            var latestReview = d.ApprovalHistory
                .OrderByDescending(a => a.ReviewedAt)
                .FirstOrDefault();

            return new EventDocumentDto
            {
                Id = d.Id,
                GroupId = d.GroupId,
                GroupNumber = d.Group?.Number,
                SenderId = d.SenderId,
                SenderName = d.Sender?.Name ?? "Desconhecido",
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                SubmittedAt = d.SubmittedAt,
                Status = d.Status,
                Feedback = latestReview?.Feedback,
                ReviewedById = latestReview?.ReviewerId,
                ReviewedByName = latestReview?.Reviewer?.Name,
                ReviewedAt = latestReview?.ReviewedAt
            };
        }
    }
}
