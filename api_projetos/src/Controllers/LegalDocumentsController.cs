using API_Gestao_Eventos.src.Application.DTO.LegalDocument;
using API_Gestao_Eventos.src.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/legal-documents")]
    public class LegalDocumentsController(LegalDocumentService legalDocumentService) : Controller
    {
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var documents = await legalDocumentService.GetCurrentDocumentsAsync();
            return Ok(documents);
        }

        [Authorize]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var pending = await legalDocumentService.GetPendingDocumentsAsync(userId);
            return Ok(pending);
        }

        [Authorize]
        [HttpPost("accept")]
        public async Task<IActionResult> Accept([FromBody] AcceptTermsRequestDto request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ipAddress = GetClientIpAddress();

            try
            {
                await legalDocumentService.AcceptAsync(userId, request.DocumentIds, ipAddress);
                return NoContent();
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

        private string GetClientIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var firstIp = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(firstIp))
                    return firstIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}