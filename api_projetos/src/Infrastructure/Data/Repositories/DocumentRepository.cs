using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;

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
    }
    public interface IDocumentRepository
    {
        Task<Document> AddAsync(Document document);
    }
}
