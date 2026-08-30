using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
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
    }
}
