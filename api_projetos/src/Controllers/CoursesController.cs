using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : Controller
    {
        private readonly CourseService _courseService;

        public CoursesController(CourseService courseService)
        {
            _courseService = courseService;
        }

        private (bool IsAdmin, bool IsSecretary, Guid? CallerInstitutionId) GetAuthContext()
        {
            var isAdmin = User.IsInRole("Administrador");
            var isSecretary = User.IsInRole("Secretaria");
            var callerInstitutionId = User.FindFirstValue("InstitutionId") is string instId
                ? Guid.Parse(instId)
                : (Guid?)null;
            return (isAdmin, isSecretary, callerInstitutionId);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SelectItemDto>>> GetAll([FromQuery] Guid? institutionId)
        {
            var result = await _courseService.GetCoursesForSelectAsync(institutionId);
            return Ok(result);
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto request)
        {
            var (isAdmin, isSecretary, callerInstitutionId) = GetAuthContext();

            try
            {
                var id = await _courseService.CreateCourseAsync(request, isAdmin, callerInstitutionId, isSecretary);
                return StatusCode(StatusCodes.Status201Created, new { id });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var (isAdmin, isSecretary, callerInstitutionId) = GetAuthContext();

            try
            {
                var course = await _courseService.GetByIdAsync(id, isAdmin, callerInstitutionId, isSecretary);
                if (course == null)
                    return NotFound(new { message = "Curso não encontrado." });

                return Ok(course);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseDto request)
        {
            var (isAdmin, isSecretary, callerInstitutionId) = GetAuthContext();

            try
            {
                await _courseService.UpdateCourseAsync(id, request, isAdmin, callerInstitutionId, isSecretary);
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

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (isAdmin, isSecretary, callerInstitutionId) = GetAuthContext();

            try
            {
                await _courseService.DeleteCourseAsync(id, isAdmin, callerInstitutionId, isSecretary);
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