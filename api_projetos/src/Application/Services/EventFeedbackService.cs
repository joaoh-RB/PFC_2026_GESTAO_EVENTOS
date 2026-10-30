using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class EventFeedbackService(IEventFeedbackRepository eventFeedbackRepository, IUserRepository userRepository)
    {
        public async Task<EventFeedbackResponseDto> CreateFeedbackAsync(Guid eventId, Guid studentId, CreateEventFeedbackRequestDto request)
        {
            var evt = await eventFeedbackRepository.GetEventAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            if (DateTime.UtcNow <= evt.EndDate)
                throw new InvalidOperationException("O feedback só pode ser enviado após o término do evento.");

            var registration = await eventFeedbackRepository.GetRegistrationAsync(eventId, studentId)
                ?? throw new InvalidOperationException("Você não está inscrito neste evento.");

            var hasAttended = registration.Status == RegistrationStatus.Attended
                || (registration.Status == RegistrationStatus.Confirmed && registration.AttendedAt != null);

            if (!hasAttended)
                throw new InvalidOperationException("Você só pode avaliar o evento se tiver presença confirmada.");

            var existingFeedback = await eventFeedbackRepository.GetByEventAndStudentAsync(eventId, studentId);
            if (existingFeedback != null)
                throw new InvalidOperationException("Você já enviou um feedback para este evento.");

            var feedback = new EventFeedback
            {
                EventId = eventId,
                StudentId = studentId,
                Rating = request.Rating,
                FeedbackText = request.FeedbackText?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await eventFeedbackRepository.AddAsync(feedback);

            var created = await eventFeedbackRepository.GetByEventAndStudentAsync(eventId, studentId) ?? feedback;
            return MapToDto(created);
        }

        public async Task<EventFeedbackResponseDto?> GetMyFeedbackAsync(Guid eventId, Guid studentId)
        {
            var evt = await eventFeedbackRepository.GetEventAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            var feedback = await eventFeedbackRepository.GetByEventAndStudentAsync(eventId, studentId);
            if (feedback == null)
                return null;

            return MapToDto(feedback);
        }

        public async Task<IEnumerable<EventFeedbackResponseDto>> GetEventFeedbacksAsync(Guid eventId, Guid userId)
        {
            await EnsureCanViewEventFeedbacksAsync(eventId, userId);

            var feedbacks = await eventFeedbackRepository.GetByEventAsync(eventId);
            return feedbacks.Select(MapToDto);
        }

        private async Task EnsureCanViewEventFeedbacksAsync(Guid eventId, Guid userId)
        {
            var evt = await eventFeedbackRepository.GetEventAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário inválido.");

            var allowed = user.Role switch
            {
                UserRole.Administrador => true,
                UserRole.Secretaria => user.InstitutionId == evt.InstitutionId,
                UserRole.Professor => user is Teacher teacher
                    && evt.AllowedCourses.Any(c => teacher.Courses.Any(tc => tc.Id == c.Id)),
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException("Você não tem permissão para visualizar os feedbacks deste evento.");
        }

        private static EventFeedbackResponseDto MapToDto(EventFeedback feedback)
        {
            return new EventFeedbackResponseDto
            {
                Id = feedback.Id,
                EventId = feedback.EventId,
                StudentId = feedback.StudentId,
                StudentName = feedback.Student?.Name ?? string.Empty,
                CourseName = feedback.Student?.Course?.Name ?? string.Empty,
                Rating = feedback.Rating,
                FeedbackText = feedback.FeedbackText,
                CreatedAt = feedback.CreatedAt
            };
        }
    }
}

