namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class PasswordResetDto
    {
        public class ForgotPasswordRequestDto
        {
            public string Email { get; set; } = string.Empty;
        }
        public class ResetPasswordRequestDto
        {
            public string Email { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
