using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InstitutionsController : Controller
    {
        private readonly InstitutionService _institutionService;

        public InstitutionsController(InstitutionService institutionService)
        {
            _institutionService = institutionService;
        }
        private (bool IsAdmin, bool IsInstitutionAdmin, Guid? CallerInstitutionId) GetAuthContext()
        {
            var isAdmin = User.IsInRole("Administrador");
            var isInstitutionAdmin = User.FindFirstValue("IsInstitutionAdmin") == "true";
            var callerInstitutionId = User.FindFirstValue("InstitutionId") is string instId
                ? Guid.Parse(instId)
                : (Guid?)null;
            return (isAdmin, isInstitutionAdmin, callerInstitutionId);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SelectItemDto>>> GetAll()
        {
            var result = await _institutionService.GetInstituctionsForSelectAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInstitutionDto request)
        {
            try
            {
                var id = await _institutionService.CreateInstitutionAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (isAdmin, isInstitutionAdmin, callerInstitutionId) = GetAuthContext();
            try
            {
                var institution = await _institutionService.GetByIdAsync(id, isAdmin, callerInstitutionId, isInstitutionAdmin);
                if (institution == null)
                    return NotFound(new { message = "Instituição não encontrada." });

                return Ok(institution);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstitutionDto request)
        {
            var (isAdmin, isInstitutionAdmin, callerInstitutionId) = GetAuthContext();
            try
            {
                await _institutionService.UpdateInstitutionAsync(id, request, isAdmin, callerInstitutionId, isInstitutionAdmin);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (isAdmin, isInstitutionAdmin, callerInstitutionId) = GetAuthContext();
            try
            {
                await _institutionService.DeleteInstitutionAsync(id, isAdmin, callerInstitutionId, isInstitutionAdmin);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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