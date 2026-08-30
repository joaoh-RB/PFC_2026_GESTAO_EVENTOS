namespace API_Gestao_Eventos.src.Infrastructure.Services.Security
{
    public interface IHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}
