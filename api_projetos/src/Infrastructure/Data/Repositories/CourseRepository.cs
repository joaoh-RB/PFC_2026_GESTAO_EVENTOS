using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class CourseRepository
    {
        private readonly AppDbContext _context;
        public CourseRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Course>> GetAllAsync()
        {
            return await _context.Courses.Where(c => c.IsActive).ToListAsync();
        }
        public async Task<IEnumerable<Course>> GetByInstitutionAsync(Guid institutionId)
        {
            return await _context.Courses.Where(c => c.InstitutionId == institutionId && c.IsActive).ToListAsync();
        }
        public async Task<Course?> GetByIdAsync(Guid id)
        {
            return await _context.Courses.FindAsync(id);
        }
        public async Task<bool> ExistsByNameAsync(string name, Guid institutionId)
        {
            return await _context.Courses.AnyAsync(c =>
                c.Name.ToLower() == name.ToLower() && c.InstitutionId == institutionId && c.IsActive);
        }
        public async Task<bool> ExistsByNameExceptAsync(string name, Guid institutionId, Guid id)
        {
            return await _context.Courses.AnyAsync(c =>
                c.Id != id && c.InstitutionId == institutionId && c.Name.ToLower() == name.ToLower() && c.IsActive);
        }
        public async Task<bool> HasDependenciesAsync(Guid id)
        {
            var hasStudents = await _context.Students.AnyAsync(s => s.CourseId == id);
            var hasTeachers = await _context.Courses
                .Where(c => c.Id == id)
                .SelectMany(c => c.Teachers)
                .AnyAsync();
            var hasEvents = await _context.Courses
                .Where(c => c.Id == id)
                .SelectMany(c => c.Events)
                .AnyAsync();
            return hasStudents || hasTeachers || hasEvents;
        }
        public async Task AddAsync(Course course)
        {
            await _context.Courses.AddAsync(course);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Course course)
        {
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Course course)
        {
            course.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
