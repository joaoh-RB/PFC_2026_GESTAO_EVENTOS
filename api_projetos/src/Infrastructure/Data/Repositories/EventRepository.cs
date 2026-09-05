using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class EventRepository(AppDbContext _context, UserRepository userRepository)
    {
        public async Task<IEnumerable<Event>> GetAllActiveAsync()
        {
            return await _context.Events.Where(e => e.IsActive).ToListAsync();
        }
        public async Task<Event?> GetByIdAsync(Guid eventId)
        {
            return await _context.Events
                .Include(e => e.Students)
                .Include(e => e.AllowedCourses)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }
        public async Task<IEnumerable<Event>> GetAllAvailableForUserAsync(Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user!.Role == Domain.Enums.UserRole.Administrador)
                return await GetAllActiveAsync();
            var query = _context.Events
                        .Where(w => w.IsActive);
            if (user is Student student)
            {
                query = query.Where(w => w.AllowedCourses.Any(c => c.Id == student.CourseId));
            }
            else if (user is Teacher teacher)
            {
                var teacherCourseIds = teacher.Courses.Select(c => c.Id).ToList();

                query = query.Where(e => e.AllowedCourses.Any(c => teacherCourseIds.Contains(c.Id)));
            }
            return await query.ToListAsync();
        }
        public async Task<Event> AddAsync(Event eventEntity)
        {
            await _context.Events.AddAsync(eventEntity);
            await _context.SaveChangesAsync();
            return eventEntity;
        }
        public async Task UpdateAsync(Event eventEntity)
        {
            _context.Events.Update(eventEntity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Guid eventId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity != null)
            {
                eventEntity.IsActive = false;
                _context.Events.Update(eventEntity);
                await _context.SaveChangesAsync();
            }
        }
    }
}