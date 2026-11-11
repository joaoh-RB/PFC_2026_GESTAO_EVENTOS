namespace API_Gestao_Eventos.src.Application.DTO.Dashboard
{
    public class DashboardFilterDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Guid? EventId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? TeacherId { get; set; }
    }

    public class DashboardQuery
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? InstitutionId { get; set; }
        public IReadOnlyCollection<Guid>? CourseIds { get; set; }
        public bool RestrictToOwnEvents { get; set; }
        public Guid? CreatorUserId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? CourseId { get; set; }
        public Guid? TeacherId { get; set; }
    }

    public class DashboardOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DashboardTeacherOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Guid> CourseIds { get; set; } = [];
    }

    public class DashboardEventOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public List<Guid> CourseIds { get; set; } = [];
        public List<Guid> TeacherIds { get; set; } = [];
    }

    public class DashboardSubmissionDto
    {
        public int Groups { get; set; }
        public int Submissions { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
    }

    public class DashboardRatingPointDto
    {
        public int Rating { get; set; }
        public int Count { get; set; }
    }

    public class DashboardFeedbackDto
    {
        public int Count { get; set; }
        public double? AverageRating { get; set; }
        public List<DashboardRatingPointDto> Distribution { get; set; } = [];
    }

    public class DashboardMonthPointDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Registrations { get; set; }
        public int Attendances { get; set; }
        public int Submissions { get; set; }
    }

    public class DashboardResponseDto
    {
        public string ScopeDescription { get; set; } = string.Empty;
        public bool CanFilterByTeacher { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Students { get; set; }
        public int PendingStudents { get; set; }
        public int RejectedStudents { get; set; }
        public int Events { get; set; }
        public int Registrations { get; set; }
        public int Attendances { get; set; }
        public DashboardSubmissionDto Submissions { get; set; } = new();
        public DashboardFeedbackDto Feedback { get; set; } = new();
        public List<DashboardMonthPointDto> Timeline { get; set; } = [];
        public List<DashboardEventOptionDto> EventOptions { get; set; } = [];
        public List<DashboardOptionDto> Courses { get; set; } = [];
        public List<DashboardTeacherOptionDto> Teachers { get; set; } = [];
    }
}
