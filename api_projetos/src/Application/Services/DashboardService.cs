using API_Gestao_Eventos.src.Application.DTO.Dashboard;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class DashboardService(IUserRepository userRepository, IDashboardRepository dashboardRepository)
    {
        public async Task<DashboardResponseDto> GetAsync(Guid userId, DashboardFilterDto filter)
        {
            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.Role == UserRole.Aluno)
                throw new InvalidOperationException("O painel analítico não está disponível para alunos.");

            var from = NormalizeUtc(filter.From ?? DateTime.UtcNow.AddMonths(-5));
            var to = NormalizeUtc(filter.To ?? DateTime.UtcNow);
            if (to < from)
                throw new InvalidOperationException("A data final deve ser posterior à data inicial.");
            if (to - from > TimeSpan.FromDays(3660))
                throw new InvalidOperationException("O período máximo do painel é de 10 anos.");

            var query = new DashboardQuery
            {
                From = from,
                To = to,
                EventId = EmptyToNull(filter.EventId),
                CourseId = EmptyToNull(filter.CourseId),
                TeacherId = EmptyToNull(filter.TeacherId)
            };

            switch (user.Role)
            {
                case UserRole.Administrador:
                    break;
                case UserRole.Secretaria:
                    if (user.InstitutionId is null)
                        return Empty(from, to, "Sua secretaria ainda não está vinculada a uma instituição.", false);
                    query.InstitutionId = user.InstitutionId;
                    break;
                case UserRole.Professor:
                    if (user.InstitutionId is null)
                        return Empty(from, to, "Seu usuário ainda não está vinculado a uma instituição.", false);
                    query.InstitutionId = user.InstitutionId;
                    query.RestrictToOwnEvents = true;
                    query.CreatorUserId = user.Id;
                    query.CourseIds = user is Teacher teacher
                        ? teacher.Courses.Select(c => c.Id).ToList()
                        : [];
                    query.TeacherId = null;
                    break;
                default:
                    throw new InvalidOperationException("Perfil sem acesso ao painel analítico.");
            }

            var response = await dashboardRepository.GetAsync(query);
            response.CanFilterByTeacher = user.Role != UserRole.Professor;
            response.ScopeDescription = user.Role switch
            {
                UserRole.Administrador => "Visão de todas as instituições.",
                UserRole.Secretaria => "Visão da sua instituição.",
                _ => "Visão dos seus cursos e dos eventos que você criou, na sua instituição."
            };
            return response;
        }

        private static DashboardResponseDto Empty(DateTime from, DateTime to, string description, bool canFilterByTeacher)
        {
            return new DashboardResponseDto
            {
                From = from,
                To = to,
                ScopeDescription = description,
                CanFilterByTeacher = canFilterByTeacher,
                Feedback = new DashboardFeedbackDto
                {
                    Distribution = Enumerable.Range(0, 11)
                        .Select(rating => new DashboardRatingPointDto { Rating = rating })
                        .ToList()
                }
            };
        }

        private static Guid? EmptyToNull(Guid? value) =>
            value is null || value == Guid.Empty ? null : value;

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
