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
        public async Task<ActionResult<IEnumerable<SelectItemDto>>> GetAll()
        {
            var result = await _courseService.GetCoursesForSelectAsync();
            return Ok(result);
        }
    }
}
