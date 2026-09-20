namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class ForgotPasswordRequestDto
    {
        public required string Email { get; set; }
    }
    public class ResetPasswordRequestDto
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
