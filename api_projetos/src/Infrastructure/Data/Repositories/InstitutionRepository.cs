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
        public async Task<Institution?> GetByIdAsync(Guid id)
        {
            return await _context.Institutions.FindAsync(id);
        }
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Institutions.AnyAsync(i => i.Name.ToLower() == name.ToLower());
        }
        public async Task<bool> ExistsByCnpjAsync(Domain.Cnpj cnpj)
        {
            return await _context.Institutions.AnyAsync(i => i.Cnpj == cnpj);
        }
        public async Task AddAsync(Institution institution)
        {
            await _context.Institutions.AddAsync(institution);
            await _context.SaveChangesAsync();
        }
    }
}
