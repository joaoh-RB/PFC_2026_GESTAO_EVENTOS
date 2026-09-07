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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SelectItemDto>>> GetAll([FromQuery] Guid? institutionId)
        {
            var result = await _courseService.GetCoursesForSelectAsync(institutionId);
            return Ok(result);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto request)
        {
            try
            {
                var id = await _courseService.CreateCourseAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var isAdmin = User.IsInRole("Administrador");
            var isInstitutionAdmin = User.FindFirstValue("IsInstitutionAdmin") == "true";
            var callerInstitutionId = User.FindFirstValue("InstitutionId") is string instId
                ? Guid.Parse(instId)
                : (Guid?)null;

            try
            {
                var course = await _courseService.GetByIdAsync(id, isAdmin, callerInstitutionId, isInstitutionAdmin);
                if (course == null)
                    return NotFound(new { message = "Curso não encontrado." });

                return Ok(course);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}