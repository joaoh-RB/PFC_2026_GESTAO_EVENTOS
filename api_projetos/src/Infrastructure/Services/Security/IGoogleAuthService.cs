namespace API_Gestao_Eventos.src.Infrastructure.Services.Security
{
    public interface IGoogleAuthService
    {
        string issuer { get; }
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secretKey);
        bool ValidateTwoFactorCode(string secretKey, string code);
    }
}
