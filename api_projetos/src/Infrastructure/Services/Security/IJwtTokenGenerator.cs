using API_Gestao_Eventos.src.Domain.Entities;

namespace API_Gestao_Eventos.src.Infrastructure.Services.Security
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
