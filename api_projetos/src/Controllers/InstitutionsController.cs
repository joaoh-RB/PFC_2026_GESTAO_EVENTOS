using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var institution = await _institutionService.GetByIdAsync(id);
            if (institution == null)
                return NotFound(new { message = "Instituição não encontrada." });

            return Ok(institution);
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstitutionDto request)
        {
            try
            {
                await _institutionService.UpdateInstitutionAsync(id, request);
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet("management")]
        public async Task<IActionResult> GetAllForManagement()
        {
            var result = await _institutionService.GetAllForManagementAsync();
            return Ok(result);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPatch("{id:guid}/active")]
        public async Task<IActionResult> SetActive(Guid id, [FromBody] ActiveRequest request)
        {
            try
            {
                await _institutionService.SetInstitutionActiveAsync(id, request.IsActive);
                return Ok(new { message = request.IsActive ? "Instituição ativada." : "Instituição inativada." });
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
        public class ActiveRequest
        {
            public bool IsActive { get; set; }
        }
    }
}