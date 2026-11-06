using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class AttendanceRepository(AppDbContext context) : IAttendanceRepository
    {
        public async Task<Event?> GetEventAsync(Guid eventId)
        {
            return await context.Events
                .Include(e => e.AllowedCourses)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }
        public async Task SetAttendanceCodeAsync(Event evt, string code)
        {
            evt.AttendanceCode = code;
            await context.SaveChangesAsync();
        }
        public async Task<EventStudent?> GetRegistrationAsync(Guid eventId, Guid studentId)
        {
            return await context.Set<EventStudent>()
                .FirstOrDefaultAsync(es => es.EventId == eventId && es.StudentId == studentId);
        }
        public async Task<IEnumerable<EventStudent>> GetRegistrationsAsync(Guid eventId)
        {
            return await context.Set<EventStudent>()
                .Where(es => es.EventId == eventId && es.Status == RegistrationStatus.Confirmed)
                .Include(es => es.Student)
                .ThenInclude(s => s.Course)
                .OrderBy(es => es.Student.Name)
                .ToListAsync();
        }
        public async Task SetAttendanceAsync(EventStudent registration, DateTime? attendedAt, AttendanceMethod? method)
        {
            registration.AttendedAt = attendedAt;
            registration.AttendanceMethod = method;
            await context.SaveChangesAsync();
        }
    }
    public interface IAttendanceRepository
    {
        Task<Event?> GetEventAsync(Guid eventId);
        Task SetAttendanceCodeAsync(Event evt, string code);
        Task<EventStudent?> GetRegistrationAsync(Guid eventId, Guid studentId);
        Task<IEnumerable<EventStudent>> GetRegistrationsAsync(Guid eventId);
        Task SetAttendanceAsync(EventStudent registration, DateTime? attendedAt, AttendanceMethod? method);
    }
}