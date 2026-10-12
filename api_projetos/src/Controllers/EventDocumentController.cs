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
    public class EventDocumentsController(EventDocumentsService eventDocumentService) : Controller
    {
        [HttpGet("{eventId:guid}")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> GetEventDocuments([FromRoute] Guid eventId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var documents = await eventDocumentService.GetEventDocumentsAsync(eventId, userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("{eventId:guid}")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddDocument([FromRoute] Guid eventId, [FromForm] IFormFile file)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var createdEvent = await eventDocumentService.AddEventDocumentAsync(eventId, userId, file);
                return StatusCode(StatusCodes.Status201Created);
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