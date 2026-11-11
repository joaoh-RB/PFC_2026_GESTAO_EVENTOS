using API_Gestao_Eventos.src.Application.DTO.Dashboard;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
    public class DashboardController(DashboardService dashboardService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DashboardFilterDto filter)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                return Ok(await dashboardService.GetAsync(userId, filter));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
