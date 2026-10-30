using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using System.Security.Cryptography;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class AttendanceService(IAttendanceRepository attendanceRepository, IUserRepository userRepository)
    {
        private const int ManualMarkingDays = 15;
        private const int CodeLength = 6;
        private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        public async Task<string> GetCodeAsync(Guid eventId, Guid userId)
        {
            var evt = await GetManageableEventAsync(eventId, userId);
            if (DateTime.UtcNow < evt.StartDate)
                throw new InvalidOperationException("O código de presença só fica disponível depois do início do evento.");
            if (string.IsNullOrEmpty(evt.AttendanceCode))
                await attendanceRepository.SetAttendanceCodeAsync(evt, GenerateCode());
            return evt.AttendanceCode!;
        }
        public async Task ConfirmWithCodeAsync(Guid eventId, Guid studentId, string code)
        {
            var evt = await attendanceRepository.GetEventAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            var registration = await attendanceRepository.GetRegistrationAsync(eventId, studentId)
                ?? throw new InvalidOperationException("Você não está inscrito neste evento.");
            if (registration.Status != RegistrationStatus.Confirmed)
                throw new InvalidOperationException("Sua inscrição neste evento foi cancelada.");
            if (registration.AttendedAt != null)
                return;
            var now = DateTime.UtcNow;
            if (now < evt.StartDate)
                throw new InvalidOperationException("A confirmação de presença só abre quando o evento começar.");
            if (now > evt.EndDate)
                throw new InvalidOperationException("O evento já terminou. Procure a organização para registrar sua presença.");
            if (string.IsNullOrEmpty(evt.AttendanceCode))
                throw new InvalidOperationException("O código de presença ainda não foi liberado pela organização.");
            if (!string.Equals(evt.AttendanceCode, code.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Código inválido.");
            await attendanceRepository.SetAttendanceAsync(registration, now, AttendanceMethod.Code);
        }
        public async Task<IEnumerable<AttendanceRegistrationDto>> GetRegistrationsAsync(Guid eventId, Guid userId)
        {
            await GetManageableEventAsync(eventId, userId);
            var registrations = await attendanceRepository.GetRegistrationsAsync(eventId);
            return registrations.Select(r => new AttendanceRegistrationDto
            {
                StudentId = r.StudentId,
                StudentName = r.Student.Name,
                CourseName = r.Student.Course.Name,
                Attended = r.AttendedAt != null,
                AttendedAt = r.AttendedAt,
                Method = r.AttendanceMethod
            });
        }
        public async Task MarkAttendanceAsync(Guid eventId, Guid studentId, Guid userId)
        {
            var registration = await GetRegistrationForManualChangeAsync(eventId, studentId, userId);
            if (registration.AttendedAt != null)
                return;
            await attendanceRepository.SetAttendanceAsync(registration, DateTime.UtcNow, AttendanceMethod.Manual);
        }
        public async Task UnmarkAttendanceAsync(Guid eventId, Guid studentId, Guid userId)
        {
            var registration = await GetRegistrationForManualChangeAsync(eventId, studentId, userId);
            if (registration.AttendedAt == null)
                return;
            await attendanceRepository.SetAttendanceAsync(registration, null, null);
        }
        private async Task<EventStudent> GetRegistrationForManualChangeAsync(Guid eventId, Guid studentId, Guid userId)
        {
            var evt = await GetManageableEventAsync(eventId, userId);
            var now = DateTime.UtcNow;
            if (now < evt.StartDate)
                throw new InvalidOperationException("A presença só pode ser registrada depois do início do evento.");
            if (now > evt.EndDate.AddDays(ManualMarkingDays))
                throw new InvalidOperationException($"O prazo de {ManualMarkingDays} dias após o término do evento para registrar presença acabou.");
            var registration = await attendanceRepository.GetRegistrationAsync(eventId, studentId)
                ?? throw new KeyNotFoundException("Inscrição não encontrada.");
            if (registration.Status != RegistrationStatus.Confirmed)
                throw new InvalidOperationException("A inscrição deste aluno foi cancelada.");
            return registration;
        }
        private async Task<Event> GetManageableEventAsync(Guid eventId, Guid userId)
        {
            var evt = await attendanceRepository.GetEventAsync(eventId)
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
                throw new InvalidOperationException("Você não tem permissão para gerenciar a presença deste evento.");
            return evt;
        }
        private static string GenerateCode()
        {
            var chars = new char[CodeLength];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = CodeChars[RandomNumberGenerator.GetInt32(CodeChars.Length)];
            return new string(chars);
        }
    }
}