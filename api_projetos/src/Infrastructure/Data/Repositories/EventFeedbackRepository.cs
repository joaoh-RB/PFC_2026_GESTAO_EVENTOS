using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class EventFeedbackRepository(AppDbContext context) : IEventFeedbackRepository
    {
        public async Task<Event?> GetEventAsync(Guid eventId)
        {
            return await context.Events
                .Include(e => e.AllowedCourses)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        public async Task<EventStudent?> GetRegistrationAsync(Guid eventId, Guid studentId)
        {
            return await context.Set<EventStudent>()
                .FirstOrDefaultAsync(es => es.EventId == eventId && es.StudentId == studentId);
        }

        public async Task<EventFeedback?> GetByEventAndStudentAsync(Guid eventId, Guid studentId)
        {
            return await context.EventFeedbacks
                .AsNoTracking()
                .Include(ef => ef.Student)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(ef => ef.EventId == eventId && ef.StudentId == studentId);
        }

        public async Task<IEnumerable<EventFeedback>> GetByEventAsync(Guid eventId)
        {
            return await context.EventFeedbacks
                .AsNoTracking()
                .Where(ef => ef.EventId == eventId)
                .Include(ef => ef.Student)
                    .ThenInclude(s => s.Course)
                .OrderByDescending(ef => ef.CreatedAt)
                .ToListAsync();
        }

        public async Task<EventFeedback> AddAsync(EventFeedback feedback)
        {
            await context.EventFeedbacks.AddAsync(feedback);
            await context.SaveChangesAsync();
            return feedback;
        }
    }

    public interface IEventFeedbackRepository
    {
        Task<Event?> GetEventAsync(Guid eventId);
        Task<EventStudent?> GetRegistrationAsync(Guid eventId, Guid studentId);
        Task<EventFeedback?> GetByEventAndStudentAsync(Guid eventId, Guid studentId);
        Task<IEnumerable<EventFeedback>> GetByEventAsync(Guid eventId);
        Task<EventFeedback> AddAsync(EventFeedback feedback);
    }
}

