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
                await eventDocumentService.AddEventDocumentAsync(eventId, userId, file);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("{documentId:guid}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadDocument([FromRoute] Guid documentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var (fileStream, contentType, fileName) = await eventDocumentService.DownloadDocumentAsync(documentId, userId);
                return File(fileStream, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = "O arquivo físico não foi encontrado no servidor de armazenamento." });
            }
        }
        [HttpDelete("{documentId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteDocument([FromRoute] Guid documentId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await eventDocumentService.DeleteDocumentAsync(documentId, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
    }
}