using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class TermAcceptanceRepository(AppDbContext _context)
    {
        public async Task<bool> HasAcceptedAsync(Guid userId, Guid legalDocumentId)
        {
            return await _context.TermAcceptances
                .AnyAsync(t => t.UserId == userId && t.LegalDocumentId == legalDocumentId);
        }
        public async Task AddAsync(TermAcceptance acceptance)
        {
            await _context.TermAcceptances.AddAsync(acceptance);
            await _context.SaveChangesAsync();
        }
    }
}