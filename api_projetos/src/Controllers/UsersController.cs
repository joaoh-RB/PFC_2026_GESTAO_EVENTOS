using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController(UserService userService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await userService.GetAllAsync());
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try { return Ok(await userService.GetByIdAsync(id)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
        [HttpPost]
        public async Task<IActionResult> Create(RegisterRequestDto request)
        {
            try
            {
                var user = await userService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserRequestDto request)
        {
            try { return Ok(await userService.UpdateAsync(id, request)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
        [HttpPatch("{id:guid}/approval")]
        public async Task<IActionResult> SetApproval(Guid id, ApprovalRequest request)
        {
            try
            {
                var approverId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await userService.SetApprovalAsync(id, request.Status, approverId);
                return Ok(new { message = request.Status == UserApprovalStatus.Aprovado ? "Usuário aprovado." : "Usuário reprovado." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
        [HttpPatch("{id:guid}/active")]
        public async Task<IActionResult> SetActive(Guid id, ActiveRequest request)
        {
            try
            {
                await userService.SetActiveAsync(id, request.IsActive);
                return Ok(new { message = request.IsActive ? "Usuário ativado." : "Usuário inativado." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await userService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception)
            {
                return Conflict(new { message = "Não foi possível excluir o usuário porque há registros vinculados. Inative-o." });
            }
        }
        public class ApprovalRequest
        {
            public UserApprovalStatus Status { get; set; }
        }
        public class ActiveRequest
        {
            public bool IsActive { get; set; }
        }
    }
}