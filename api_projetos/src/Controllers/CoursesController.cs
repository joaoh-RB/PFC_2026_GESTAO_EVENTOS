using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [Authorize(Roles = "Administrador,Secretaria")]
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

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var course = await _courseService.GetByIdAsync(id);
            if (course == null)
                return NotFound(new { message = "Curso não encontrado." });

            return Ok(course);
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseDto request)
        {
            try
            {
                await _courseService.UpdateCourseAsync(id, request);
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

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpGet("management")]
        public async Task<IActionResult> GetForManagement([FromQuery] Guid institutionId)
        {
            var result = await _courseService.GetForManagementAsync(institutionId);
            return Ok(result);
        }

        [Authorize(Roles = "Administrador,Secretaria")]
        [HttpPatch("{id:guid}/active")]
        public async Task<IActionResult> SetActive(Guid id, [FromBody] ActiveRequest request)
        {
            try
            {
                await _courseService.SetCourseActiveAsync(id, request.IsActive);
                return Ok(new { message = request.IsActive ? "Curso ativado." : "Curso inativado." });
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