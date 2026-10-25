using System.ComponentModel.DataAnnotations;

namespace API_Gestao_Eventos.src.Application.DTO.Event
{
    public class CreateEventFeedbackRequestDto
    {
        [Range(0, 10, ErrorMessage = "A nota deve estar entre 0 e 10.")]
        public int Rating { get; set; }

        [MaxLength(1000, ErrorMessage = "O comentário não pode exceder 1000 caracteres.")]
        public string FeedbackText { get; set; } = string.Empty;
    }
}

