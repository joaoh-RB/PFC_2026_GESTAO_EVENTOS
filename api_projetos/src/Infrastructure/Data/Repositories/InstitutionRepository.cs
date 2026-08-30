using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class InstitutionRepository
    {
        private readonly AppDbContext _context;
        public InstitutionRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Institution>> GetAllAsync()
        {
            return await _context.Institutions.ToListAsync();
        }
    }
}
