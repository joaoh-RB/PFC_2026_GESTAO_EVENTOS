using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class GroupService(IGroupRepository groupRepository, IEventRepository eventRepository, IUserRepository userRepository)
    {
        public async Task<Guid> CreateGroupAsync(Guid leaderId, CreateGroupRequestDto request)
        {
            var evt = await eventRepository.GetByIdAsync(request.EventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (!evt.IsActive)
                throw new InvalidOperationException("Não é possível criar grupo em um evento inativo.");
            if (!evt.AllowDocuments)
                throw new InvalidOperationException("Este evento não permite envio de documentos.");
            if (!await groupRepository.IsStudentRegisteredAsync(leaderId, evt.Id))
                throw new InvalidOperationException("Você precisa estar inscrito no evento para criar um grupo.");
            if (await groupRepository.HasAcceptedGroupAsync(leaderId, evt.Id))
                throw new InvalidOperationException("Você já participa de um grupo neste evento.");
            var invitedIds = request.StudentIds.Where(id => id != leaderId).Distinct().ToList();
            await ValidateInvitedAsync(evt.Id, invitedIds);
            if (evt.MaxGroupSize.HasValue && invitedIds.Count + 1 > evt.MaxGroupSize.Value)
                throw new InvalidOperationException($"O grupo pode ter no máximo {evt.MaxGroupSize} alunos.");
            var group = new Domain.Entities.Group
            {
                EventId = evt.Id,
                LeaderId = leaderId,
                Number = await groupRepository.GetNextNumberAsync(evt.Id),
                IsActive = true
            };
            group.Members.Add(new GroupMember { StudentId = leaderId, EventId = evt.Id, Status = GroupMemberStatus.Accepted });
            foreach (var studentId in invitedIds)
                group.Members.Add(new GroupMember { StudentId = studentId, EventId = evt.Id });
            await groupRepository.AddAsync(group);
            await groupRepository.DeclinePendingInvitesAsync(leaderId, evt.Id, group.Id);
            return group.Id;
        }
        public async Task InviteStudentsAsync(Guid groupId, Guid userId, InviteGroupMembersRequestDto request)
        {
            var group = await GetGroupForLeaderAsync(groupId, userId);
            var studentIds = request.StudentIds.Where(id => id != group.LeaderId).Distinct().ToList();
            await ValidateInvitedAsync(group.EventId, studentIds);
            var evt = await eventRepository.GetByIdAsync(group.EventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (evt.MaxGroupSize.HasValue)
            {
                var reserved = group.Members.Count(m => m.Status == GroupMemberStatus.Accepted || m.Status == GroupMemberStatus.Invited);
                var newInvites = studentIds.Count(id => !group.Members.Any(m => m.StudentId == id && m.Status == GroupMemberStatus.Invited));
                if (reserved + newInvites > evt.MaxGroupSize.Value)
                    throw new InvalidOperationException($"O grupo pode ter no máximo {evt.MaxGroupSize} alunos.");
            }
            foreach (var studentId in studentIds)
            {
                var existing = group.Members.FirstOrDefault(m => m.StudentId == studentId);
                if (existing == null)
                    await groupRepository.AddMemberAsync(new GroupMember { GroupId = group.Id, StudentId = studentId, EventId = group.EventId });
                else if (existing.Status == GroupMemberStatus.Declined)
                    await groupRepository.SetMemberStatusAsync(existing, GroupMemberStatus.Invited);
            }
        }
        public async Task RemoveMemberAsync(Guid groupId, Guid userId, Guid studentId)
        {
            var group = await GetGroupForLeaderAsync(groupId, userId);
            if (studentId == group.LeaderId)
                throw new InvalidOperationException("O líder não pode ser removido do grupo.");
            var member = group.Members.FirstOrDefault(m => m.StudentId == studentId)
                ?? throw new KeyNotFoundException("Membro não encontrado no grupo.");
            await groupRepository.RemoveMemberAsync(member);
        }
        public async Task AcceptInviteAsync(Guid groupId, Guid studentId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            if (!group.IsActive)
                throw new KeyNotFoundException("Grupo não encontrado.");
            var invite = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Invited)
                ?? throw new KeyNotFoundException("Convite não encontrado.");
            if (!await groupRepository.IsStudentRegisteredAsync(studentId, group.EventId))
                throw new InvalidOperationException("Você precisa estar inscrito no evento para aceitar o convite.");
            if (await groupRepository.HasAcceptedGroupAsync(studentId, group.EventId))
                throw new InvalidOperationException("Você já participa de um grupo neste evento.");
            var evt = await eventRepository.GetByIdAsync(group.EventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (!HasVacancy(group, evt))
                throw new InvalidOperationException("Este grupo já está cheio.");
            await groupRepository.SetMemberStatusAsync(invite, GroupMemberStatus.Accepted);
            await groupRepository.DeclinePendingInvitesAsync(studentId, group.EventId, group.Id);
        }
        public async Task DeclineInviteAsync(Guid groupId, Guid studentId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            if (!group.IsActive)
                throw new KeyNotFoundException("Grupo não encontrado.");
            var invite = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Invited)
                ?? throw new KeyNotFoundException("Convite não encontrado.");
            await groupRepository.SetMemberStatusAsync(invite, GroupMemberStatus.Declined);
        }
        public async Task LeaveGroupAsync(Guid groupId, Guid studentId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            var member = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Accepted)
                ?? throw new KeyNotFoundException("Você não participa deste grupo.");
            var evt = await eventRepository.GetByIdAsync(group.EventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (DateTime.UtcNow >= evt.StartDate)
                throw new InvalidOperationException("Não é possível sair do grupo depois que o evento começou.");
            if (group.LeaderId == studentId)
            {
                var nextLeader = group.Members
                    .Where(m => m.StudentId != studentId && m.Status == GroupMemberStatus.Accepted)
                    .OrderBy(m => m.JoinedAt)
                    .FirstOrDefault();
                if (nextLeader == null)
                {
                    await groupRepository.RemoveMemberAsync(member);
                    await groupRepository.DeleteAsync(group.Id);
                    return;
                }
                group.LeaderId = nextLeader.StudentId;
            }
            await groupRepository.RemoveMemberAsync(member);
        }
        public async Task<IEnumerable<OpenGroupDto>> GetOpenGroupsAsync(Guid eventId, Guid studentId)
        {
            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (!evt.IsActive || !evt.AllowDocuments || DateTime.UtcNow >= evt.StartDate)
                return new List<OpenGroupDto>();
            if (!await groupRepository.IsStudentRegisteredAsync(studentId, eventId)
                || await groupRepository.HasAcceptedGroupAsync(studentId, eventId))
                return new List<OpenGroupDto>();

            var groups = await groupRepository.GetActiveByEventAsync(eventId);
            return groups
                .Where(g => HasVacancy(g, evt))
                .Where(g => !g.Members.Any(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Invited))
                .Select(g =>
                {
                    var accepted = g.Members.Count(m => m.Status == GroupMemberStatus.Accepted);
                    return new OpenGroupDto
                    {
                        Id = g.Id,
                        Number = g.Number,
                        LeaderName = g.Members.FirstOrDefault(m => m.StudentId == g.LeaderId)?.Student?.Name ?? string.Empty,
                        MembersCount = accepted,
                        MaxSize = evt.MaxGroupSize,
                        AvailableSlots = evt.MaxGroupSize.HasValue ? evt.MaxGroupSize.Value - accepted : null,
                        HasPendingRequest = g.Members.Any(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Requested)
                    };
                })
                .ToList();
        }
        public async Task RequestToJoinAsync(Guid groupId, Guid studentId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            if (!group.IsActive)
                throw new KeyNotFoundException("Grupo não encontrado.");
            var evt = await GetOpenEventAsync(group.EventId);
            if (!await groupRepository.IsStudentRegisteredAsync(studentId, group.EventId))
                throw new InvalidOperationException("Você precisa estar inscrito no evento para pedir entrada em um grupo.");
            if (await groupRepository.HasAcceptedGroupAsync(studentId, group.EventId))
                throw new InvalidOperationException("Você já participa de um grupo neste evento.");
            if (!HasVacancy(group, evt))
                throw new InvalidOperationException("Este grupo já está cheio.");
            var existing = group.Members.FirstOrDefault(m => m.StudentId == studentId);
            if (existing == null)
                await groupRepository.AddMemberAsync(new GroupMember { GroupId = group.Id, StudentId = studentId, EventId = group.EventId, Status = GroupMemberStatus.Requested });
            else if (existing.Status == GroupMemberStatus.Requested)
                throw new InvalidOperationException("Você já enviou um pedido para este grupo.");
            else if (existing.Status == GroupMemberStatus.Invited)
                throw new InvalidOperationException("Você já foi convidado para este grupo. Aceite o convite.");
            else
                await groupRepository.SetMemberStatusAsync(existing, GroupMemberStatus.Requested);
        }
        public async Task CancelRequestAsync(Guid groupId, Guid studentId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            var request = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Requested)
                ?? throw new KeyNotFoundException("Pedido não encontrado.");
            await groupRepository.RemoveMemberAsync(request);
        }
        public async Task ApproveRequestAsync(Guid groupId, Guid leaderId, Guid studentId)
        {
            var group = await GetGroupForLeaderAsync(groupId, leaderId);
            var evt = await GetOpenEventAsync(group.EventId);
            var request = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Requested)
                ?? throw new KeyNotFoundException("Pedido não encontrado.");
            if (await groupRepository.HasAcceptedGroupAsync(studentId, group.EventId))
                throw new InvalidOperationException("Este aluno já entrou em outro grupo.");
            if (!HasVacancy(group, evt))
                throw new InvalidOperationException("O grupo já está cheio.");
            await groupRepository.SetMemberStatusAsync(request, GroupMemberStatus.Accepted);
            await groupRepository.DeclinePendingInvitesAsync(studentId, group.EventId, group.Id);
        }
        public async Task RejectRequestAsync(Guid groupId, Guid leaderId, Guid studentId)
        {
            var group = await GetGroupForLeaderAsync(groupId, leaderId);
            var request = group.Members.FirstOrDefault(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Requested)
                ?? throw new KeyNotFoundException("Pedido não encontrado.");
            await groupRepository.RemoveMemberAsync(request);
        }
        public async Task<GroupResponseDto?> GetMyGroupAsync(Guid eventId, Guid studentId)
        {
            var group = await groupRepository.GetGroupForStudentInEventAsync(studentId, eventId);
            return group == null ? null : MapToDto(group);
        }
        public async Task<IEnumerable<GroupResponseDto>> GetMyInvitesAsync(Guid studentId)
        {
            var groups = await groupRepository.GetInvitesForStudentAsync(studentId);
            return groups.Select(MapToDto);
        }
        public async Task<IEnumerable<GroupResponseDto>> GetEventGroupsAsync(Guid eventId, Guid userId)
        {
            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");

            var user = await userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user is Teacher teacher)
            {
                var allowedForTeacher = evt.AllowedCourses.Any(c => teacher.Courses.Any(tc => tc.Id == c.Id));
                if (!allowedForTeacher)
                    throw new InvalidOperationException("Este evento não pertence aos seus cursos.");
            }
            else if (user is not Administrator && user is not AcademicDepartment)
            {
                throw new InvalidOperationException("Você não possui permissão para visualizar os grupos deste evento.");
            }

            var groups = await groupRepository.GetByEventAsync(eventId);
            return groups.Select(MapToDto);
        }
        public async Task<IEnumerable<SelectItemDto>> GetInvitableStudentsAsync(Guid eventId, Guid userId)
        {
            var students = await groupRepository.GetStudentsAvailableForGroupAsync(eventId);
            return students.Where(s => s.Id != userId).Select(s => new SelectItemDto
            {
                Value = s.Id.ToString(),
                Label = s.Name
            });
        }
        private async Task ValidateInvitedAsync(Guid eventId, List<Guid> studentIds)
        {
            if (studentIds.Count == 0) return;
            var available = (await groupRepository.GetStudentsAvailableForGroupAsync(eventId)).Select(s => s.Id).ToHashSet();
            if (studentIds.Any(id => !available.Contains(id)))
                throw new InvalidOperationException("Só é possível convidar alunos inscritos no evento que ainda não estão em um grupo.");
        }
        private async Task<Event> GetOpenEventAsync(Guid eventId)
        {
            var evt = await eventRepository.GetByIdAsync(eventId)
                ?? throw new KeyNotFoundException("Evento não encontrado.");
            if (DateTime.UtcNow >= evt.StartDate)
                throw new InvalidOperationException("Os grupos não podem mais ser alterados, pois o evento já começou.");
            return evt;
        }
        private static bool HasVacancy(Domain.Entities.Group group, Event evt)
        {
            return evt.MaxGroupSize == null
                || group.Members.Count(m => m.Status == GroupMemberStatus.Accepted) < evt.MaxGroupSize.Value;
        }
        private async Task<Domain.Entities.Group> GetGroupForLeaderAsync(Guid groupId, Guid userId)
        {
            var group = await groupRepository.GetByIdAsync(groupId)
                ?? throw new KeyNotFoundException("Grupo não encontrado.");
            if (group.LeaderId != userId)
                throw new InvalidOperationException("Somente o líder pode alterar o grupo.");
            return group;
        }
        private static GroupResponseDto MapToDto(Domain.Entities.Group g)
        {
            return new GroupResponseDto
            {
                Id = g.Id,
                EventId = g.EventId,
                EventName = g.Event?.Name ?? string.Empty,
                Number = g.Number,
                LeaderId = g.LeaderId,
                LeaderName = g.Members.FirstOrDefault(m => m.StudentId == g.LeaderId)?.Student?.Name ?? string.Empty,
                Members = g.Members.Select(m => new GroupMemberDto
                {
                    StudentId = m.StudentId,
                    Name = m.Student?.Name ?? string.Empty,
                    Status = m.Status,
                    IsLeader = m.StudentId == g.LeaderId
                }).ToList()
            };
        }
    }
}