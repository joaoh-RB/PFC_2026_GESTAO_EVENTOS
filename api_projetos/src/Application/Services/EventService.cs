using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.GoogleCalendar;
using System.Net.WebSockets;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventService(IEventRepository eventRepository, ICourseRepository courseRepository, IGoogleCalendarService googleCalendarService, IAuthService authService)
    {
        public async Task<Event> AddAsync(CreateEventRequestDto request)
        {
            var allowedCourses = (await courseRepository.GetByInstitutionAsync(request.InstitutionId)).Where(w => request.AllowedCourses.Contains(w.Id)).ToList();
            if (allowedCourses.Count != request.AllowedCourses.Count)
            {
                throw new InvalidOperationException("Nem todos os cursos cadastrados são permitidos.");
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
            string googleEventId = await googleCalendarService.CreateEventAsync(newEvent);
            newEvent.GoogleCalendarEventId = googleEventId;
            return await eventRepository.AddAsync(newEvent);
        }
        public async Task UpdateAsync(Guid id, CreateEventRequestDto request)
        {
            var allowedCourses = (await courseRepository.GetByInstitutionAsync(request.InstitutionId)).Where(w => request.AllowedCourses.Contains(w.Id)).ToList();
            if (allowedCourses.Count != request.AllowedCourses.Count)
            {
                throw new InvalidOperationException("Nem todos os cursos cadastrados pertencem à instituição.");
            }
            var eventCreated = await eventRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Evento não encontrado.");
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
                        .ToList(),
                GoogleCalendarEventId = eventCreated.GoogleCalendarEventId
            };
            await googleCalendarService.UpdateEventAsync(eventToUpdate);
            await eventRepository.UpdateAsync(eventToUpdate);
        }
        public async Task SetActiveAsync(Guid id, bool isActive)
        {
            var eventToUpdate = await eventRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (!isActive && eventToUpdate.StartDate < DateTime.UtcNow)
                throw new InvalidOperationException("Não é possível inativar um evento que já começou.");
            await googleCalendarService.DeactivateEventAsync(eventToUpdate.GoogleCalendarEventId, isActive);
            await eventRepository.SetActiveAsync(eventToUpdate, isActive);
        }
        public async Task<PagedResponseDto<EventResponseDto>> GetFilteredEventsPagedAsync(EventFilterDto parameters, Guid userId)
        {
            var (events, totalCount) = await eventRepository.GetFilteredAvailableForUserAsync(parameters, userId);
            var additionsToCalendar = await eventRepository.GetAllAdditionsToCalendarAsync(userId);
            var items = events.Select(e =>
            {
                var confirmedRegistrations = e.EventStudents.Count(s => s.Status == RegistrationStatus.Confirmed);
                var availableSeats = Math.Max(e.Capacity - confirmedRegistrations, 0);

                return new EventResponseDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    InstitutionId = e.InstitutionId,
                    InstitutionName = e.Institution.Name.ToString(),
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Capacity = e.Capacity,
                    ConfirmedRegistrations = confirmedRegistrations,
                    AvailableSeats = availableSeats,
                    IsFull = availableSeats == 0,
                    EventType = (int)e.EventType,
                    AllowDocuments = e.AllowDocuments,
                    AllowedCourseNames = e.AllowedCourses.Select(c => c.Name.ToString()).ToList(),
                    AllowedCourseIds = e.AllowedCourses.Select(c => c.Id).ToList(),
                    IsActive = e.IsActive,
                    IsAddedToCalendar = additionsToCalendar.Any(a => a.EventId == e.Id),
                    AllowsAddToCalendar = e.GoogleCalendarEventId != null,
                    IsUserRegistered = e.EventStudents.Any(s => s.StudentId == userId && s.Status == RegistrationStatus.Confirmed),
                    IsUserAttended = e.EventStudents.Any(s => s.StudentId == userId && s.AttendedAt != null)
                };
            });

            return new PagedResponseDto<EventResponseDto>
            {
                Items = items,
                TotalItems = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            };
        }
        public async Task AddUserToCalendarAsync(Guid eventId, Guid userId)
        {

            var user = await authService.GetUserInfoAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            if (string.IsNullOrEmpty(evt.GoogleCalendarEventId))
                throw new InvalidOperationException("Este evento não possui integração com Google Calendar.");

            await googleCalendarService.AddAttendeeAsync(evt.GoogleCalendarEventId, user.Email);
            var eventAddedToCalendar = await eventRepository.GetEventAddedToCalendarAsync(eventId, userId);
            if (eventAddedToCalendar == null)
            {
                eventAddedToCalendar = new EventAddedToCalendar
                {
                    EventId = eventId,
                    UserId = userId,
                    IsActive = true,
                    AddedAt = DateTime.UtcNow
                };
                await eventRepository.AddEventToCalendarAsync(eventAddedToCalendar);
            }
            else if (!eventAddedToCalendar.IsActive)
            {
                eventAddedToCalendar.IsActive = true;
                await eventRepository.UpdateEventInCalendarAsync(eventAddedToCalendar);
            }
        }
        public async Task RemoveUserFromCalendarAsync(Guid eventId, Guid userId)
        {

            var user = await authService.GetUserInfoAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            if (string.IsNullOrEmpty(evt.GoogleCalendarEventId))
                throw new InvalidOperationException("Este evento não possui integração com Google Calendar.");

            await googleCalendarService.RemoveAttendeeAsync(evt.GoogleCalendarEventId, user.Email);
            var eventAddedToCalendar = await eventRepository.GetEventAddedToCalendarAsync(eventId, userId);
            if (eventAddedToCalendar != null)
            {
                eventAddedToCalendar.IsActive = false;
                await eventRepository.UpdateEventInCalendarAsync(eventAddedToCalendar);
            }
        }
        public async Task JoinEventAsync(Guid eventId, Guid userId)
        {
            var user = await authService.GetUserInfoAsync(userId) ?? throw new KeyNotFoundException("Usuário inválido.");
            var evt = (await eventRepository.GetFilteredAvailableForUserAsync(new EventFilterDto()
            {
                FromDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = int.MaxValue,
                InstitutionId = user.InstitutionId
            }, userId)).Item1.FirstOrDefault(f => f.Id == eventId)
                ?? throw new KeyNotFoundException("Evento inválido para inscrição.");
            if (!evt.IsActive)
                throw new InvalidOperationException("Não é possível se inscrever em um evento inativo.");
            var confirmedRegistrations = evt.EventStudents.Count(s => s.Status == RegistrationStatus.Confirmed);
            var existingRegistration = evt.EventStudents.FirstOrDefault(f => f.StudentId == userId);
            if (existingRegistration is null && confirmedRegistrations >= evt.Capacity)
                throw new InvalidOperationException("Não é possível se inscrever neste evento, pois a capacidade máxima foi atingida.");
            if (existingRegistration != null)
            {
                if (existingRegistration.Status == RegistrationStatus.Confirmed)
                    throw new InvalidOperationException("Você já está inscrito neste evento.");
                if (confirmedRegistrations >= evt.Capacity)
                    throw new InvalidOperationException("Não é possível se inscrever neste evento, pois a capacidade máxima foi atingida.");

                existingRegistration.Status = RegistrationStatus.Confirmed;
                existingRegistration.RegisteredAt = DateTime.UtcNow;
                existingRegistration.CanceledAt = null;
                await eventRepository.UpdateAsync(evt);
            }
            else
            {
                var newRegistration = new EventStudent
                {
                    EventId = eventId,
                    StudentId = userId,
                    Status = RegistrationStatus.Confirmed,
                    RegisteredAt = DateTime.UtcNow
                };
                evt.EventStudents.Add(newRegistration);
                await eventRepository.UpdateAsync(evt);
            }
        }
        public async Task LeaveEventAsync(Guid eventId, Guid userId)
        {
            var user = await authService.GetUserInfoAsync(userId) ?? throw new KeyNotFoundException("Usuário inválido.");
            var evt = (await eventRepository.GetFilteredAvailableForUserAsync(new EventFilterDto()
            {
                FromDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = int.MaxValue,
                InstitutionId = user.InstitutionId
            }, userId)).Item1.FirstOrDefault(f => f.Id == eventId)
                ?? throw new KeyNotFoundException("Evento inválido para saída.");
            var existingRegistration = evt.EventStudents.FirstOrDefault(f => f.StudentId == userId);
            if (existingRegistration == null || existingRegistration.Status != RegistrationStatus.Confirmed)
                throw new InvalidOperationException("Você não está inscrito neste evento.");
            existingRegistration.Status = RegistrationStatus.Canceled;
            existingRegistration.CanceledAt = DateTime.UtcNow;
            if (evt.GoogleCalendarEventId != null && user.Email != null)
                await googleCalendarService.RemoveAttendeeAsync(evt.GoogleCalendarEventId, user.Email);
            await eventRepository.UpdateAsync(evt);
        }

        public async Task<PagedResponseDto<EventRegistrationDto>> GetRegistrationsAsync(Guid eventId, Guid userId, EventRegistrationFilterDto filter)
        {
            var user = await authService.GetUserInfoAsync(userId) ?? throw new KeyNotFoundException("Usuário inválido.");
            var evt = (await eventRepository.GetFilteredAvailableForUserAsync(new EventFilterDto()
            {
                FromDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = int.MaxValue,
                InstitutionId = user.InstitutionId
            }, userId)).Item1.FirstOrDefault(f => f.Id == eventId)
                ?? throw new KeyNotFoundException("Evento inválido para consulta.");

            var registrations = evt.EventStudents
                .Where(s => s.Status == RegistrationStatus.Confirmed)
                .OrderBy(s => s.RegisteredAt)
                .Select(s => new EventRegistrationDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.Student.Name,
                    CourseName = s.Student.Course.Name,
                    RegisteredAt = s.RegisteredAt
                });

            var totalItems = registrations.Count();
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize < 1 ? 10 : Math.Min(filter.PageSize, 50);
            var items = registrations
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResponseDto<EventRegistrationDto>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}