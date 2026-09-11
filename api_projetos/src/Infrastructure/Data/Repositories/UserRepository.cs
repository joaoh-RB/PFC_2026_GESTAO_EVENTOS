using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
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
            var teacher = await _context.Users
                .OfType<Teacher>()
                .Include(t => t.Courses)
                .Include(i => i.Institution)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (teacher != null)
                return teacher;

            return await _context.Users.Include(i => i.Institution)
                .FirstOrDefaultAsync(u => u.Id == id);
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
        public async Task<(IEnumerable<User>, int totalItems)> GetFilteredInstitutionMembersAsync(
            InstitutionMemberFilterDto institutionMemberFilterDto)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(e => e.Institution)
                .Where(e =>
                    (e.Role == UserRole.Professor || e.Role == UserRole.Secretaria));

            if (institutionMemberFilterDto.UserRole.HasValue)
            {
                query = query.Where(e => e.Role == institutionMemberFilterDto.UserRole);
            }

            if (institutionMemberFilterDto.InstitutionId.HasValue &&
                institutionMemberFilterDto.InstitutionId.Value != Guid.Empty)
            {
                query = query.Where(e => e.InstitutionId == institutionMemberFilterDto.InstitutionId.Value);
            }

            var totalCount = await query.CountAsync();

            var pageNumber = institutionMemberFilterDto.PageNumber < 1
                ? 1
                : institutionMemberFilterDto.PageNumber;

            var pageSize = institutionMemberFilterDto.PageSize < 1
                ? 6
                : Math.Min(institutionMemberFilterDto.PageSize, 30);


            var items = await query
                .OrderByDescending(e => e.IsActive)
                .ThenBy(e => e.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var teacherIds = items
                .OfType<Teacher>()
                .Select(t => t.Id)
                .ToList();


            if (teacherIds.Any())
            {
                var teachers = await _context.Users
                    .OfType<Teacher>()
                    .Include(t => t.Courses)
                    .Where(t => teacherIds.Contains(t.Id))
                    .ToListAsync();


                foreach (var teacher in teachers)
                {
                    var user = items.First(u => u.Id == teacher.Id);

                    if (user is Teacher userTeacher)
                    {
                        userTeacher.Courses = teacher.Courses;
                    }
                }
            }

            return (items, totalCount);
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
