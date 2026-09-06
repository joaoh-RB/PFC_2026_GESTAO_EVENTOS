using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController(EventService eventService) : Controller
    {
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador))]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequestDto request)
        {
            try
            {
                var createdEvent = await eventService.AddAsync(request);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetEvents()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var events = await eventService.GetAllAvailableForUserAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("types")]
        public IActionResult GetEventTypes()
        {
            try
            {
                var eventTypes = Enum.GetValues<EventType>().Cast<EventType>().Select(s => new SelectItemDto() { Label = s.ToString(), Value = ((int)s).ToString() });
                return Ok(eventTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
