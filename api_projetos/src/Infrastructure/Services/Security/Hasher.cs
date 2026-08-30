using BCrypt.Net;
using static BCrypt.Net.BCrypt;
namespace API_Gestao_Eventos.src.Infrastructure.Services.Security
{
    public class Hasher : IHasher
    {
        private const int WorkFactor = 12;
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("A senha não pode ser vazia.", nameof(password));
            return EnhancedHashPassword(password, WorkFactor, HashType.SHA384);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            return EnhancedVerify(password, passwordHash, HashType.SHA384);
        }
    }
}
