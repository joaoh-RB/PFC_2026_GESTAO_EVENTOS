namespace API_Gestao_Eventos.src.Application.DTO.Auth
{
    public class TwoFactorCreateResponseDto
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
    }
}
