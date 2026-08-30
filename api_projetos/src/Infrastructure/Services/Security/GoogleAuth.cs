using OtpNet;

namespace API_Gestao_Eventos.src.Infrastructure.Services.Security
{
    public class GoogleAuth : IGoogleAuthService
    {
        public string issuer { get; } = "API_Gestao_Eventos";
        public string GenerateSecretKey()
        {
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretBytes);
        }
        public string GenerateQrCodeUri(string email, string secretKey)
        {
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);

            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}";
        }
        public bool ValidateTwoFactorCode(string secretKey, string code)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var secretBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(secretBytes);
                return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
            }
            catch
            {
                return false;
            }
        }
    }
}
