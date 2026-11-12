namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? TwoFactorCode { get; set; }
    }
}
