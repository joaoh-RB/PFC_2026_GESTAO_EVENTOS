using API_Gestao_Eventos.src.Application.DTO.Event;
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
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == eventEntity.Id);

            if (currentEvent == null)
                throw new KeyNotFoundException("Evento não encontrado.");

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
        public async Task<(IEnumerable<Event>, int totalItems)> GetFilteredAvailableForUserAsync(EventFilterDto eventFilterDto, Guid userId)
        {
            var user = await userRepository.GetByIdAsync(userId);
            IQueryable<Event> query = _context.Events
                        .AsNoTracking()
                        .Include(e => e.Institution)
                        .Include(e => e.AllowedCourses)
                        .Include(e => e.EventStudents);
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
                query = query.Where(e => e.IsActive && e.AllowedCourses.Any(c => c.Id == student.CourseId));
            }
            else if (user is Teacher teacher)
            {
                var teacherCourseIds = teacher.Courses.Select(c => c.Id).ToList();
                query = query.Where(e => e.AllowedCourses.Any(c => teacherCourseIds.Contains(c.Id)));
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
    }
}