using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace API_Gestao_Eventos.src.Infrastructure.Services
{
    public static class AuditHelper
    {
        public static Guid? GetUserIdFromHttpContext(IHttpContextAccessor? httpContextAccessor)
        {
            try
            {
                var ctx = httpContextAccessor?.HttpContext;
                if (ctx == null) return null;
                var claim = ctx.User?.FindFirst(ClaimTypes.NameIdentifier) ?? ctx.User?.FindFirst("sub");
                if (claim == null) return null;
                if (Guid.TryParse(claim.Value, out var id)) return id;
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string SerializeObject(object? obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
    }
}
