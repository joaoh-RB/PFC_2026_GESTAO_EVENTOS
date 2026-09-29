namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class ChangeInitialPasswordDto
    {
        public required string Email { get; set; }
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
