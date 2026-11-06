using System.ComponentModel.DataAnnotations;

namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class EventFeedbackResponseDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;

        [Range(0, 10)]
        public int Rating { get; set; }

        public string FeedbackText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

