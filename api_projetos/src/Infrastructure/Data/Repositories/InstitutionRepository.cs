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
        public async Task<bool> ExistsByNameExceptAsync(string name, Guid id)
        {
            return await _context.Institutions.AnyAsync(i =>
                i.Id != id && i.Name.ToLower() == name.ToLower());
        }
        public async Task<bool> ExistsByCnpjExceptAsync(Domain.Cnpj cnpj, Guid id)
        {
            return await _context.Institutions.AnyAsync(i =>
                i.Id != id && i.Cnpj == cnpj);
        }
        public async Task<bool> HasDependenciesAsync(Guid id)
        {
            var hasUsers = await _context.Users.AnyAsync(u => u.InstitutionId == id);
            var hasCourses = await _context.Courses.AnyAsync(c => c.InstitutionId == id);
            var hasEvents = await _context.Events.AnyAsync(e => e.InstitutionId == id);
            return hasUsers || hasCourses || hasEvents;
        }
        public async Task AddAsync(Institution institution)
        {
            await _context.Institutions.AddAsync(institution);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Institution institution)
        {
            _context.Institutions.Update(institution);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Institution institution)
        {
            institution.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
