using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class LegalDocumentRepository(AppDbContext _context)
    {
        public async Task<LegalDocument?> GetActiveByTypeAsync(LegalDocumentType type)
        {
            return await _context.LegalDocuments
                .FirstOrDefaultAsync(d => d.Type == type && d.IsActive);
        }

        public async Task<IEnumerable<LegalDocument>> GetAllActiveAsync()
        {
            return await _context.LegalDocuments
                .Where(d => d.IsActive)
                .ToListAsync();
        }

        public async Task<LegalDocument?> GetByIdAsync(Guid id)
        {
            return await _context.LegalDocuments.FindAsync(id);
        }
    }
}