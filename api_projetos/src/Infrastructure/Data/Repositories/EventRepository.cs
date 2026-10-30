using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class EventRepository(AppDbContext _context, IUserRepository userRepository) : IEventRepository
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
                .Include(e => e.CreatedByUser)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }
        public async Task<Event> AddAsync(Event eventEntity)
        {
            await _context.Events.AddAsync(eventEntity);
            await _context.SaveChangesAsync();
            return eventEntity;
        }
        public async Task UpdateAsync(Event eventEntity)
        {
            var currentEvent = await _context.Events
                .Include(e => e.EventAllowedCourses)
                .Include(e => e.EventStudents)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == eventEntity.Id);

            if (currentEvent == null)
                throw new KeyNotFoundException("Evento não encontrado.");
            if (eventEntity.EventStudents != null)
            {
                foreach (var registration in eventEntity.EventStudents)
                {
                    var current = currentEvent.EventStudents
                        .FirstOrDefault(s => s.StudentId == registration.StudentId);
                    if (current == null)
                    {
                        registration.Student = null!;
                        currentEvent.EventStudents.Add(registration);
                    }
                    else
                    {
                        _context.Entry(current).CurrentValues.SetValues(registration);
                    }
                }
            }
            _context.Entry(currentEvent).CurrentValues.SetValues(eventEntity);

            var targetCourseIds = eventEntity.AllowedCourses.Select(c => c.Id).ToHashSet();

            var linksToDeactivate = currentEvent
                                    .EventAllowedCourses
                                    .Where(link => link.IsActive && !targetCourseIds.Contains(link.CourseId));

            foreach (var link in linksToDeactivate)
            {
                link.IsActive = false;
            }

            foreach (var courseId in targetCourseIds)
            {
                var existingLink = currentEvent.EventAllowedCourses
                    .FirstOrDefault(link => link.CourseId == courseId);

                if (existingLink != null)
                {
                    if (!existingLink.IsActive)
                    {
                        existingLink.IsActive = true;
                    }
                }
                else
                {
                    currentEvent.EventAllowedCourses.Add(new EventAllowedCourse
                    {
                        EventId = currentEvent.Id,
                        CourseId = courseId,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
        public async Task SetActiveAsync(Event eventEntity, bool isActive)
        {
            eventEntity.IsActive = isActive;
            await _context.SaveChangesAsync();
        }
        public async Task SetApprovalStatusAsync(Event eventEntity, EventStatus status, string? googleCalendarEventId = null)
        {
            eventEntity.EventStatus = status;
            if (googleCalendarEventId != null)
            {
                eventEntity.GoogleCalendarEventId = googleCalendarEventId;
            }
            await _context.SaveChangesAsync();
        }
        public async Task<(IEnumerable<Event>, int totalItems)> GetFilteredAvailableForUserAsync(EventFilterDto eventFilterDto, Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId);
            IQueryable<Event> query = _context.Events
                        .AsNoTracking()
                        .Include(e => e.Institution)
                        .Include(e => e.CreatedByUser)
                        .Include(e => e.AllowedCourses)
                        .Include(e => e.EventStudents)
                            .ThenInclude(es => es.Student)
                                .ThenInclude(s => s.Course)
                        .Where(w => w.Institution.IsActive);
            if (eventFilterDto.FromDate.HasValue)
            {
                var utcFromDate = DateTime.SpecifyKind(eventFilterDto.FromDate.Value, DateTimeKind.Utc);
                query = query.Where(e => e.StartDate >= utcFromDate);
            }
            if (eventFilterDto.InstitutionId.HasValue && eventFilterDto.InstitutionId.Value != Guid.Empty)
            {
                query = query.Where(e => e.InstitutionId == eventFilterDto.InstitutionId.Value);
            }
            if (eventFilterDto.IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == eventFilterDto.IsActive.Value);
            }
            if (user is Student student)
            {
                query = query.Where(e => e.IsActive && e.EventStatus == EventStatus.Agendado && e.AllowedCourses.Any(c => c.Id == student.CourseId));
            }
            else
            {
                var targetStatus = eventFilterDto.EventStatus ?? EventStatus.Agendado;
                query = query.Where(e => e.EventStatus == targetStatus);

                if (user is Teacher teacher)
                {
                    var teacherCourseIds = teacher.Courses.Select(c => c.Id).ToList();
                    query = query.Where(e => e.AllowedCourses.Any(c => teacherCourseIds.Contains(c.Id)));
                }
            }
            var totalCount = await query.CountAsync();
            var pageNumber = eventFilterDto.PageNumber < 1 ? 1 : eventFilterDto.PageNumber;
            var pageSize = eventFilterDto.PageSize < 1 ? 6 : Math.Min(eventFilterDto.PageSize, 30);
            var items = await query
                .OrderBy(e => e.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (items, totalCount);
        }
        public async Task<IEnumerable<EventAddedToCalendar>> GetAllAdditionsToCalendarAsync(Guid userId)
        {
            return await _context.EventAddedToCalendars
                .Where(eac => eac.UserId == userId)
                .Include(eac => eac.Event)
                .ToListAsync();
        }
        public async Task<EventAddedToCalendar?> GetEventAddedToCalendarAsync(Guid eventId, Guid userId)
        {
            return await _context.EventAddedToCalendars.IgnoreQueryFilters()
                          .FirstOrDefaultAsync(eac => eac.EventId == eventId && eac.UserId == userId);
        }
        public async Task AddEventToCalendarAsync(EventAddedToCalendar eventAddedToCalendar)
        {
            await _context.EventAddedToCalendars.AddAsync(eventAddedToCalendar);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateEventInCalendarAsync(EventAddedToCalendar eventAddedToCalendar)
        {
            _context.EventAddedToCalendars.Update(eventAddedToCalendar);
            await _context.SaveChangesAsync();
        }
        public async Task<HashSet<Guid>> GetEventIdsWithApprovedDocumentsAsync(IEnumerable<Guid> eventIds)
        {
            var idList = eventIds.ToList();
            if (idList.Count == 0)
                return new HashSet<Guid>();

            var ids = await _context.Documents
                .AsNoTracking()
                .Where(d => d.Group != null && idList.Contains(d.Group.EventId) && d.Status == DocumentStatus.Approved)
                .Select(d => d.Group!.EventId)
                .Distinct()
                .ToListAsync();

            return ids.ToHashSet();
        }
    }
    public interface IEventRepository
    {
        Task<IEnumerable<Event>> GetAllActiveAsync();
        Task<Event?> GetByIdAsync(Guid eventId);
        Task<Event> AddAsync(Event eventEntity);
        Task UpdateAsync(Event eventEntity);
        Task SetActiveAsync(Event eventEntity, bool isActive);
        Task SetApprovalStatusAsync(Event eventEntity, EventStatus status, string? googleCalendarEventId = null);
        Task<(IEnumerable<Event>, int totalItems)> GetFilteredAvailableForUserAsync(EventFilterDto eventFilterDto, Guid userId);
        Task<IEnumerable<EventAddedToCalendar>> GetAllAdditionsToCalendarAsync(Guid userId);
        Task<EventAddedToCalendar?> GetEventAddedToCalendarAsync(Guid eventId, Guid userId);
        Task AddEventToCalendarAsync(EventAddedToCalendar eventAddedToCalendar);
        Task UpdateEventInCalendarAsync(EventAddedToCalendar eventAddedToCalendar);
        Task<HashSet<Guid>> GetEventIdsWithApprovedDocumentsAsync(IEnumerable<Guid> eventIds);
    }
}
