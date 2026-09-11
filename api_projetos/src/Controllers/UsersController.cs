using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador,Professor,Secretaria")]
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
                var user = await userService.CreateStudentAsync(request);
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
        #region INstitution Members
        [HttpGet("roles/institution-members")]
        public IActionResult GetInstitutionMembersUserRoles()
        {
            try
            {
                var userRoles = Enum.GetValues<UserRole>().Cast<UserRole>().Where(w => w == UserRole.Professor || w == UserRole.Secretaria).Select(s => new SelectItemDto() { Label = s.ToString(), Value = ((int)s).ToString() });
                return Ok(userRoles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("institution-members")]
        [Authorize(Roles = nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> GetInstitutionMembers([FromQuery] InstitutionMemberFilterDto filter)
        {
            try
            {
                var users = await userService.GetInstitutionMembersPagedAsync(filter);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("institution-members")]
        [Authorize(Roles = nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> CreateInstitutionMember([FromBody] CreateInstitutionMemberRequestDto createInstitutionMemberRequestDto)
        {
            try
            {
                await userService.CreateInstitutionMember(createInstitutionMemberRequestDto);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("institution-members/{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> UpdateInstitutionMember(Guid id, [FromBody] UpdateInstitutionMemberRequestDto updateInstitutionMemberRequestDto)
        {
            try
            {
                await userService.UpdateInstitutionMemberAsync(id, updateInstitutionMemberRequestDto);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #endregion
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