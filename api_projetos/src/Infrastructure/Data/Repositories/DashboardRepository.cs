using API_Gestao_Eventos.src.Application.DTO.Dashboard;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class DashboardRepository(AppDbContext context) : IDashboardRepository
    {
        public async Task<DashboardResponseDto> GetAsync(DashboardQuery query)
        {
            await EnsureFiltersAreInScopeAsync(query);

            var baseEvents = ScopedEvents(query);
            var institutionStudents = context.Students.AsNoTracking().AsQueryable();
            if (query.InstitutionId is Guid studentInstitutionId)
                institutionStudents = institutionStudents.Where(s => s.InstitutionId == studentInstitutionId);

            var approvedStudents = await institutionStudents.CountAsync(s => s.ApprovalStatus == UserApprovalStatus.Aprovado);
            var pendingStudents = await institutionStudents.CountAsync(s => s.ApprovalStatus == UserApprovalStatus.Pendente);
            var rejectedStudents = await institutionStudents.CountAsync(s => s.ApprovalStatus == UserApprovalStatus.Reprovado);

            var periodIds = await baseEvents
                .Where(e => e.StartDate <= query.To && e.EndDate >= query.From)
                .Select(e => e.Id)
                .ToListAsync();
            var periodRegistrations = Registrations(periodIds)
                .Where(es => es.RegisteredAt >= query.From && es.RegisteredAt <= query.To);
            var registrationCount = await periodRegistrations.CountAsync();

            var filteredEvents = ApplyDimensionFilters(baseEvents, query);
            var factEventIds = query.EventId.HasValue
                ? await filteredEvents.Select(e => e.Id).ToListAsync()
                : await filteredEvents
                    .Where(e => e.StartDate <= query.To && e.EndDate >= query.From)
                    .Select(e => e.Id)
                    .ToListAsync();

            var registrations = Registrations(factEventIds)
                .Where(es => es.RegisteredAt >= query.From && es.RegisteredAt <= query.To);
            var attendanceCount = await Registrations(factEventIds)
                .CountAsync(es => es.AttendedAt != null && es.AttendedAt >= query.From && es.AttendedAt <= query.To);

            var documents = DocumentsInPeriod(factEventIds, query.From, query.To);
            var documentStatuses = await documents
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var submissions = new DashboardSubmissionDto
            {
                Groups = await context.Groups.AsNoTracking().CountAsync(g =>
                    g.IsActive &&
                    factEventIds.Contains(g.EventId) &&
                    g.CreatedAt >= query.From &&
                    g.CreatedAt <= query.To),
                Submissions = documentStatuses.Sum(s => s.Count),
                Approved = documentStatuses.FirstOrDefault(s => s.Status == DocumentStatus.Approved)?.Count ?? 0,
                Rejected = documentStatuses.FirstOrDefault(s => s.Status == DocumentStatus.Rejected)?.Count ?? 0,
                Pending = documentStatuses.FirstOrDefault(s => s.Status == DocumentStatus.Pending)?.Count ?? 0
            };

            var feedbackQuery = context.EventFeedbacks.AsNoTracking()
                .Where(f => factEventIds.Contains(f.EventId) && f.CreatedAt >= query.From && f.CreatedAt <= query.To);
            var feedbackCount = await feedbackQuery.CountAsync();
            var ratingGroups = await feedbackQuery
                .GroupBy(f => f.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();
            var distribution = Enumerable.Range(1, 5)
                .Select(rating => new DashboardRatingPointDto
                {
                    Rating = rating,
                    Count = ratingGroups.FirstOrDefault(g => g.Rating == rating)?.Count ?? 0
                })
                .ToList();

            var registrationMonths = await registrations
                .GroupBy(es => new { es.RegisteredAt.Year, es.RegisteredAt.Month })
                .Select(g => new MonthCount(g.Key.Year, g.Key.Month, g.Count()))
                .ToListAsync();
            var attendanceMonths = await Registrations(factEventIds)
                .Where(es => es.AttendedAt != null && es.AttendedAt >= query.From && es.AttendedAt <= query.To)
                .GroupBy(es => new { es.AttendedAt!.Value.Year, es.AttendedAt!.Value.Month })
                .Select(g => new MonthCount(g.Key.Year, g.Key.Month, g.Count()))
                .ToListAsync();
            var submissionMonths = await documents
                .GroupBy(d => new { d.SubmittedAt.Year, d.SubmittedAt.Month })
                .Select(g => new MonthCount(g.Key.Year, g.Key.Month, g.Count()))
                .ToListAsync();

            var options = await LoadOptionsAsync(baseEvents, query);

            return new DashboardResponseDto
            {
                From = query.From,
                To = query.To,
                Students = approvedStudents,
                PendingStudents = pendingStudents,
                RejectedStudents = rejectedStudents,
                Events = periodIds.Count,
                Registrations = registrationCount,
                Attendances = attendanceCount,
                Submissions = submissions,
                Feedback = new DashboardFeedbackDto
                {
                    Count = feedbackCount,
                    AverageRating = feedbackCount == 0
                        ? null
                        : Math.Round(distribution.Sum(d => d.Rating * (double)d.Count) / feedbackCount, 1),
                    Distribution = distribution
                },
                Timeline = BuildTimeline(query.From, query.To, registrationMonths, attendanceMonths, submissionMonths),
                EventOptions = options.Events,
                Courses = options.Courses,
                Teachers = options.Teachers
            };
        }

        private async Task EnsureFiltersAreInScopeAsync(DashboardQuery query)
        {
            if (query.CourseId is Guid courseId)
            {
                var courseAllowed = await context.Courses.AsNoTracking().AnyAsync(c =>
                    c.Id == courseId &&
                    c.IsActive &&
                    (query.InstitutionId == null || c.InstitutionId == query.InstitutionId) &&
                    (query.CourseIds == null || query.CourseIds.Contains(c.Id)));
                if (!courseAllowed)
                    throw new InvalidOperationException("Curso fora do seu acesso.");
            }

            if (query.TeacherId is Guid teacherId)
            {
                var teacherAllowed = await context.Teachers.AsNoTracking().AnyAsync(t =>
                    t.Id == teacherId &&
                    t.IsActive &&
                    (query.InstitutionId == null || t.InstitutionId == query.InstitutionId));
                if (!teacherAllowed)
                    throw new InvalidOperationException("Professor fora do seu acesso.");
            }

            if (query.EventId is Guid eventId)
            {
                var eventAllowed = await ApplyDimensionFilters(ScopedEvents(query), query)
                    .AnyAsync(e => e.Id == eventId);
                if (!eventAllowed)
                    throw new InvalidOperationException("Evento fora do seu acesso.");
            }
        }

        private IQueryable<Event> ScopedEvents(DashboardQuery query)
        {
            var events = context.Events.AsNoTracking().Where(e => e.Institution.IsActive);

            if (query.InstitutionId is Guid institutionId)
                events = events.Where(e => e.InstitutionId == institutionId);

            if (query.RestrictToOwnEvents)
            {
                var courseIds = query.CourseIds?.ToList() ?? [];
                var creatorId = query.CreatorUserId;
                events = events.Where(e =>
                    (creatorId != null && e.CreatedByUserId == creatorId) ||
                    e.AllowedCourses.Any(c => courseIds.Contains(c.Id)));
            }

            return events;
        }

        private static IQueryable<Event> ApplyDimensionFilters(IQueryable<Event> events, DashboardQuery query)
        {
            if (query.CourseId is Guid courseId)
                events = events.Where(e => e.AllowedCourses.Any(c => c.Id == courseId));

            if (query.TeacherId is Guid teacherId)
            {
                events = events.Where(e =>
                    e.CreatedByUserId == teacherId ||
                    e.AllowedCourses.Any(c => c.Teachers.Any(t => t.Id == teacherId)));
            }

            if (query.EventId is Guid eventId)
                events = events.Where(e => e.Id == eventId);

            return events;
        }

        private IQueryable<EventStudent> Registrations(List<Guid> eventIds)
        {
            return context.Events.AsNoTracking()
                .Where(e => eventIds.Contains(e.Id))
                .SelectMany(e => e.EventStudents)
                .Where(es => es.Status == RegistrationStatus.Confirmed || es.Status == RegistrationStatus.Attended);
        }

        private IQueryable<Document> DocumentsInPeriod(List<Guid> eventIds, DateTime from, DateTime to)
        {
            return context.Documents.AsNoTracking()
                .Where(d =>
                    d.SubmittedAt >= from &&
                    d.SubmittedAt <= to &&
                    context.Groups.Any(g => g.Id == d.GroupId && eventIds.Contains(g.EventId)));
        }

        private async Task<(List<DashboardEventOptionDto> Events, List<DashboardOptionDto> Courses, List<DashboardTeacherOptionDto> Teachers)> LoadOptionsAsync(
            IQueryable<Event> baseEvents,
            DashboardQuery query)
        {
            var events = await baseEvents
                .OrderByDescending(e => e.StartDate)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.StartDate,
                    e.CreatedByUserId,
                    CourseIds = e.AllowedCourses.Select(c => c.Id).ToList()
                })
                .Take(400)
                .ToListAsync();

            var coursesQuery = context.Courses.AsNoTracking().Where(c => c.IsActive);
            if (query.InstitutionId is Guid institutionId)
                coursesQuery = coursesQuery.Where(c => c.InstitutionId == institutionId);
            if (query.CourseIds != null)
            {
                var allowed = query.CourseIds.ToList();
                coursesQuery = coursesQuery.Where(c => allowed.Contains(c.Id));
            }

            var courses = await coursesQuery
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, TeacherIds = c.Teachers.Where(t => t.IsActive).Select(t => t.Id).ToList() })
                .ToListAsync();

            var teachersQuery = context.Teachers.AsNoTracking().Where(t => t.IsActive);
            if (query.InstitutionId is Guid teacherInstitutionId)
                teachersQuery = teachersQuery.Where(t => t.InstitutionId == teacherInstitutionId);
            if (query.RestrictToOwnEvents && query.CreatorUserId is Guid ownId)
                teachersQuery = teachersQuery.Where(t => t.Id == ownId);

            var teachers = await teachersQuery
                .OrderBy(t => t.Name)
                .Select(t => new DashboardTeacherOptionDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    CourseIds = t.Courses.Select(c => c.Id).ToList()
                })
                .ToListAsync();

            var teachersByCourse = courses.ToDictionary(c => c.Id, c => c.TeacherIds);
            var knownTeachers = teachers.Select(t => t.Id).ToHashSet();
            var eventOptions = events.Select(e =>
            {
                var teacherIds = e.CourseIds
                    .SelectMany(courseId => teachersByCourse.GetValueOrDefault(courseId) ?? [])
                    .ToHashSet();
                if (e.CreatedByUserId is Guid creatorId && knownTeachers.Contains(creatorId))
                    teacherIds.Add(creatorId);

                return new DashboardEventOptionDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    StartDate = e.StartDate,
                    CourseIds = e.CourseIds,
                    TeacherIds = teacherIds.ToList()
                };
            }).ToList();

            return (
                eventOptions,
                courses.Select(c => new DashboardOptionDto { Id = c.Id, Name = c.Name }).ToList(),
                teachers);
        }

        private static List<DashboardMonthPointDto> BuildTimeline(
            DateTime from,
            DateTime to,
            List<MonthCount> registrations,
            List<MonthCount> attendances,
            List<MonthCount> submissions)
        {
            var registrationMap = registrations.ToDictionary(item => (item.Year, item.Month), item => item.Count);
            var attendanceMap = attendances.ToDictionary(item => (item.Year, item.Month), item => item.Count);
            var submissionMap = submissions.ToDictionary(item => (item.Year, item.Month), item => item.Count);
            var points = new List<DashboardMonthPointDto>();
            var cursor = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (cursor <= end && points.Count < 121)
            {
                var key = (cursor.Year, cursor.Month);
                points.Add(new DashboardMonthPointDto
                {
                    Year = cursor.Year,
                    Month = cursor.Month,
                    Registrations = registrationMap.GetValueOrDefault(key),
                    Attendances = attendanceMap.GetValueOrDefault(key),
                    Submissions = submissionMap.GetValueOrDefault(key)
                });
                cursor = cursor.AddMonths(1);
            }

            return points;
        }

        private sealed record MonthCount(int Year, int Month, int Count);
    }

    public interface IDashboardRepository
    {
        Task<DashboardResponseDto> GetAsync(DashboardQuery query);
    }
}
