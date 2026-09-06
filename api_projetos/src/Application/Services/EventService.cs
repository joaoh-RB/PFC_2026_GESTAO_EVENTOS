using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventService(EventRepository eventRepository, CourseRepository courseRepository)
    {
        public async Task<Event> AddAsync(CreateEventRequestDto request)
        {
            var allowedCourses = (await courseRepository.GetAllAsync()).Where(w => request.AllowedCourses.Contains(w.Id)).ToList();
            if (allowedCourses.Count != request.AllowedCourses.Count)
            {
                throw new Exception("Nem todos os cursos cadastrados são permitidos.");
            }
            var newEvent = new Event
            {
                Name = request.Name,
                Description = request.Description,
                InstitutionId = request.InstitutionId,
                StartDate = request.StartDate.UtcDateTime,
                EndDate = request.EndDate.UtcDateTime,
                Capacity = request.Capacity,
                AllowDocuments = request.AllowDocuments,
                EventType = request.EventType,
                AllowedCourses = allowedCourses
            };
            return await eventRepository.AddAsync(newEvent);
        }
        public async Task UpdateAsync(Guid id, CreateEventRequestDto request)
        {
            var eventToUpdate = new Event
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                StartDate = request.StartDate.UtcDateTime,
                EndDate = request.EndDate.UtcDateTime,
                Capacity = request.Capacity,
                EventType = request.EventType,
                AllowDocuments = request.AllowDocuments,
                InstitutionId = request.InstitutionId,
                AllowedCourses = (request.AllowedCourses ?? new List<Guid>())
                        .Select(courseId => new Course { Id = courseId })
                        .ToList()
            };
            await eventRepository.UpdateAsync(eventToUpdate);
        }
        public async Task DeleteAsync(Guid id)
        {
            var eventToDelete = await eventRepository.GetByIdAsync(id);
            if (eventToDelete!.StartDate < DateTime.UtcNow)
                throw new InvalidOperationException("Não é possível excluir um evento que já começou.");
            await eventRepository.DeleteAsync(eventToDelete);
        }
        public async Task<PagedResponseDto<EventResponseDto>> GetFilteredEventsPagedAsync(EventFilterDto parameters, Guid userId)
        {
            var (events, totalCount) = await eventRepository.GetFilteredAvailableForUserAsync(parameters, userId);

            var items = events.Select(e => new EventResponseDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                InstitutionId = e.InstitutionId,
                InstitutionName = e.Institution.Id.ToString(),
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Capacity = e.Capacity,
                ConfirmedRegistrations = e.EventStudents.Count(s => s.Status == RegistrationStatus.Confirmed),
                EventType = (int)e.EventType,
                AllowDocuments = e.AllowDocuments,
                AllowedCourseNames = e.AllowedCourses.Select(c => c.Id.ToString()).ToList(),
                AllowedCourseIds = e.AllowedCourses.Select(c => c.Id).ToList()
            });

            return new PagedResponseDto<EventResponseDto>
            {
                Items = items,
                TotalItems = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            };
        }
        public async Task<IEnumerable<Event>> GetAllActiveAsync()
        {
            return await eventRepository.GetAllActiveAsync();
        }
        public async Task<IEnumerable<Event>> GetAllAvailableForUserAsync(Guid userId)
        {
            return await eventRepository.GetAllAvailableForUserAsync(userId);
        }
    }
}