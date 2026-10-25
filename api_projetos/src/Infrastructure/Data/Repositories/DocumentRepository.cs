using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class DocumentRepository(AppDbContext context) : IDocumentRepository
    {
        public async Task<Document> AddAsync(Document document)
        {
            await context.Documents.AddAsync(document);
            await context.SaveChangesAsync();
            return document;
        }

        public async Task<DocumentApprovalHistory> AddApprovalAsync(DocumentApprovalHistory approval)
        {
            await context.DocumentApprovalHistories.AddAsync(approval);
            await context.SaveChangesAsync();
            return approval;
        }

        public async Task<Document?> GetByIdAsync(Guid documentId)
        {
            return await context.Documents
                .Include(d => d.Sender)
                .Include(d => d.Group).ThenInclude(g => g.Members)
                .Include(d => d.Group).ThenInclude(g => g.Event).ThenInclude(e => e.AllowedCourses)
                .Include(d => d.ApprovalHistory).ThenInclude(a => a.Reviewer)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<List<Document>> GetPendingByEventAsync(Guid eventId)
        {
            return await context.Documents
                .AsNoTracking()
                .Include(d => d.Sender)
                .Include(d => d.Group).ThenInclude(g => g.Members)
                .Where(d => d.Group != null && d.Group.EventId == eventId && d.Status == DocumentStatus.Pending)
                .OrderBy(d => d.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Document>> GetApprovedByEventAsync(Guid eventId)
        {
            return await context.Documents
                .AsNoTracking()
                .Include(d => d.Sender)
                .Include(d => d.Group)
                .Include(d => d.ApprovalHistory).ThenInclude(a => a.Reviewer)
                .Where(d => d.Group != null && d.Group.EventId == eventId && d.Status == DocumentStatus.Approved)
                .OrderByDescending(d => d.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Document>> GetReviewedByEventAsync(Guid eventId)
        {
            return await context.Documents
                .AsNoTracking()
                .Include(d => d.Sender)
                .Include(d => d.Group)
                .Include(d => d.ApprovalHistory).ThenInclude(a => a.Reviewer)
                .Where(d => d.Group != null && d.Group.EventId == eventId && d.Status != DocumentStatus.Pending)
                .OrderByDescending(d => d.SubmittedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Document document)
        {
            context.Documents.Update(document);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Document document)
        {
            context.Documents.Remove(document);
            await context.SaveChangesAsync();
        }
    }

    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document document);
        Task<DocumentApprovalHistory> AddApprovalAsync(DocumentApprovalHistory approval);
        Task<Document?> GetByIdAsync(Guid documentId);
        Task<List<Document>> GetPendingByEventAsync(Guid eventId);
        Task<List<Document>> GetApprovedByEventAsync(Guid eventId);
        Task<List<Document>> GetReviewedByEventAsync(Guid eventId);
        Task UpdateAsync(Document document);
        Task DeleteAsync(Document document);
    }
}
