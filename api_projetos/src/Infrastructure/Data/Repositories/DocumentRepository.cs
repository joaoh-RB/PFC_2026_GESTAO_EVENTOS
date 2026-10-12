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
        public async Task<Document?> GetByIdAsync(Guid documentId)
        {
            return await context.Documents.Include(g => g.Group).ThenInclude(g => g.Members).FirstOrDefaultAsync(f => f.Id == documentId);
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
        Task<Document?> GetByIdAsync(Guid documentId);
        Task DeleteAsync(Document document);
    }
}
