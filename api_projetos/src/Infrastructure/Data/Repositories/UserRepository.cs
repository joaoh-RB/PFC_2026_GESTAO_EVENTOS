using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<Student?> GetStudentByIdAsync(Guid id)
        {
            return await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Student>?> GetAllStudentsAsync()
        {
            return await _context.Students.AsNoTracking().Include(s => s.ApprovedByUser).OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
        public async Task<bool> ExistsByEmailExceptAsync(string email, Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id != userId && u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> ExistsByUniqueIdentifierAndInstitutionAsync(string uniqueIdentifier, Guid institutionId)
        {
            return await _context.Users.OfType<Student>().AnyAsync(s => s.UniqueIdentifier == uniqueIdentifier && institutionId == s.InstitutionId);
        }
        public async Task<bool> ExistsByUniqueIdentifierAndInstitutionExceptAsync(string uniqueIdentifier, Guid institutionId, Guid userId)
        {
            return await _context.Students.AnyAsync(s =>
                s.Id != userId &&
                s.UniqueIdentifier == uniqueIdentifier &&
                s.InstitutionId == institutionId);
        }
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
