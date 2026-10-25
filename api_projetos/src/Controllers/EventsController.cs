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
    public class EventsController(EventService eventService, AttendanceService attendanceService) : Controller
    {
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
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
        [HttpPut("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> UpdateEvent([FromRoute] Guid id, [FromBody] CreateEventRequestDto request)
        {
            try
            {
                await eventService.UpdateAsync(id, request);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetEvents([FromQuery] EventFilterDto eventFilterDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var events = await eventService.GetFilteredEventsPagedAsync(eventFilterDto, userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/active")]
        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> SetActive([FromRoute] Guid id, [FromBody] ActiveRequest request)
        {
            try
            {
                await eventService.SetActiveAsync(id, request.IsActive);
                return Ok(new { message = request.IsActive ? "Evento ativado." : "Evento inativado." });
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
        [Authorize]
        [HttpPost("{id:guid}/add-to-calendar")]
        public async Task<IActionResult> AddToCalendar(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await eventService.AddUserToCalendarAsync(id, userId);
                return Ok(new { message = "Evento adicionado à sua agenda do Google com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize]
        [HttpPost("{id:guid}/remove-from-calendar")]
        public async Task<IActionResult> RemoveFromCalendar(Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await eventService.RemoveUserFromCalendarAsync(id, userId);
                return Ok(new { message = "Evento removido da sua agenda do Google com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles = nameof(UserRole.Aluno))]
        [HttpPost("{id:guid}/join")]
        public async Task<IActionResult> JoinEvent([FromRoute] Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await eventService.JoinEventAsync(id, userId);
                return Ok(new { message = "Você se inscreveu no evento com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles = nameof(UserRole.Aluno))]
        [HttpPost("{id:guid}/leave")]
        public async Task<IActionResult> LeaveEvent([FromRoute] Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await eventService.LeaveEventAsync(id, userId);
                return Ok(new { message = "Você saiu do evento com sucesso!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        [HttpGet("{id:guid}/registrations")]
        public async Task<IActionResult> GetRegistrations([FromRoute] Guid id, [FromQuery] EventRegistrationFilterDto filter)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var registrations = await eventService.GetRegistrationsAsync(id, userId, filter);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        [HttpGet("{id:guid}/attendance/code")]
        public async Task<IActionResult> GetAttendanceCode([FromRoute] Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var code = await attendanceService.GetCodeAsync(id, userId);
                return Ok(new { code });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        [HttpGet("{id:guid}/attendance")]
        public async Task<IActionResult> GetAttendance([FromRoute] Guid id)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var registrations = await attendanceService.GetRegistrationsAsync(id, userId);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Aluno))]
        [HttpPost("{id:guid}/attendance/confirm")]
        public async Task<IActionResult> ConfirmAttendance([FromRoute] Guid id, [FromBody] ConfirmAttendanceRequestDto request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await attendanceService.ConfirmWithCodeAsync(id, userId, request.Code);
                return Ok(new { message = "Presença confirmada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        [HttpPost("{id:guid}/attendance/{studentId:guid}")]
        public async Task<IActionResult> MarkAttendance([FromRoute] Guid id, [FromRoute] Guid studentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await attendanceService.MarkAttendanceAsync(id, studentId, userId);
                return Ok(new { message = "Presença registrada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        [HttpDelete("{id:guid}/attendance/{studentId:guid}")]
        public async Task<IActionResult> UnmarkAttendance([FromRoute] Guid id, [FromRoute] Guid studentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await attendanceService.UnmarkAttendanceAsync(id, studentId, userId);
                return Ok(new { message = "Presença removida." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        public class ActiveRequest
        {
            public bool IsActive { get; set; }
        }
    }
}