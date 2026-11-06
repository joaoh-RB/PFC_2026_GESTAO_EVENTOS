using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API_Gestao_Eventos.src.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController(GroupService groupService) : Controller
    {
        private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequestDto request)
        {
            try
            {
                var id = await groupService.CreateGroupAsync(UserId, request);
                return StatusCode(StatusCodes.Status201Created, new { id });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("event/{eventId:guid}/mine")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> GetMyGroup([FromRoute] Guid eventId)
        {
            var group = await groupService.GetMyGroupAsync(eventId, UserId);
            if (group == null)
                return NotFound(new { message = "Você não participa de nenhum grupo neste evento." });
            return Ok(group);
        }

        [HttpGet("event/{eventId:guid}/students")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> GetInvitableStudents([FromRoute] Guid eventId)
        {
            return Ok(await groupService.GetInvitableStudentsAsync(eventId, UserId));
        }

        [HttpGet("event/{eventId:guid}")]
        [Authorize(Roles = nameof(UserRole.Professor) + "," + nameof(UserRole.Administrador) + "," + nameof(UserRole.Secretaria))]
        public async Task<IActionResult> GetEventGroups([FromRoute] Guid eventId)
        {
            try
            {
                return Ok(await groupService.GetEventGroupsAsync(eventId, UserId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("invites")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> GetMyInvites()
        {
            return Ok(await groupService.GetMyInvitesAsync(UserId));
        }

        [HttpPost("{id:guid}/members")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> InviteStudents([FromRoute] Guid id, [FromBody] InviteGroupMembersRequestDto request)
        {
            try
            {
                await groupService.InviteStudentsAsync(id, UserId, request);
                return Ok(new { message = "Convites enviados." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("{id:guid}/members/{studentId:guid}")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> RemoveMember([FromRoute] Guid id, [FromRoute] Guid studentId)
        {
            try
            {
                await groupService.RemoveMemberAsync(id, UserId, studentId);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/accept")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> AcceptInvite([FromRoute] Guid id)
        {
            try
            {
                await groupService.AcceptInviteAsync(id, UserId);
                return Ok(new { message = "Convite aceito." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/decline")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> DeclineInvite([FromRoute] Guid id)
        {
            try
            {
                await groupService.DeclineInviteAsync(id, UserId);
                return Ok(new { message = "Convite recusado." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/leave")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> Leave([FromRoute] Guid id)
        {
            try
            {
                await groupService.LeaveGroupAsync(id, UserId);
                return Ok(new { message = "Você saiu do grupo." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("event/{eventId:guid}/open")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> GetOpenGroups([FromRoute] Guid eventId)
        {
            try
            {
                return Ok(await groupService.GetOpenGroupsAsync(eventId, UserId));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/requests")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> RequestToJoin([FromRoute] Guid id)
        {
            try
            {
                await groupService.RequestToJoinAsync(id, UserId);
                return Ok(new { message = "Pedido enviado ao líder do grupo." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("{id:guid}/requests")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> CancelRequest([FromRoute] Guid id)
        {
            try
            {
                await groupService.CancelRequestAsync(id, UserId);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("{id:guid}/requests/{studentId:guid}/approve")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> ApproveRequest([FromRoute] Guid id, [FromRoute] Guid studentId)
        {
            try
            {
                await groupService.ApproveRequestAsync(id, UserId, studentId);
                return Ok(new { message = "Pedido aprovado." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("{id:guid}/requests/{studentId:guid}")]
        [Authorize(Roles = nameof(UserRole.Aluno))]
        public async Task<IActionResult> RejectRequest([FromRoute] Guid id, [FromRoute] Guid studentId)
        {
            try
            {
                await groupService.RejectRequestAsync(id, UserId, studentId);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}