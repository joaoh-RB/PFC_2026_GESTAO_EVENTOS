using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventService(EventRepository eventRepository, CourseRepository courseRepository)
    {
        public async Task<Event> AddAsync(CreateEventRequestDto request)
        {
            var allowedCourses = courseRepository.GetAllAsync().Result.Where(w => request.AllowedCourses.Contains(w.Id)).ToList();
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