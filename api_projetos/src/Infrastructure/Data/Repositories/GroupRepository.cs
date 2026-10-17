using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Repositories
{
    public class GroupRepository(AppDbContext context) : IGroupRepository
    {
        public async Task<Group?> GetGroupForStudentInEventAsync(Guid studentId, Guid eventId)
        {
            return await context.Groups
                .Where(g => g.EventId == eventId && g.Members.Any(s => s.StudentId == studentId && s.Status == GroupMemberStatus.Accepted))
                .Include(g => g.Event)
                .Include(g => g.Members)
                .ThenInclude(m => m.Student)
                .Include(g => g.Documents)
                .ThenInclude(g => g.Sender)
                .FirstOrDefaultAsync();
        }
        public async Task<Group?> GetByIdAsync(Guid groupId)
        {
            return await context.Groups
                .Include(g => g.Members)
                .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }
        public async Task<Group> AddAsync(Group group)
        {
            await context.Groups.AddAsync(group);
            await context.SaveChangesAsync();
            return group;
        }
        public async Task<int> GetNextNumberAsync(Guid eventId)
        {
            var last = await context.Groups
                .Where(g => g.EventId == eventId)
                .MaxAsync(g => (int?)g.Number);
            return (last ?? 0) + 1;
        }
        public async Task<IEnumerable<Group>> GetByEventAsync(Guid eventId)
        {
            return await context.Groups
                .AsNoTracking()
                .Where(g => g.EventId == eventId && g.IsActive)
                .Include(g => g.Leader)
                .Include(g => g.Members)
                .ThenInclude(m => m.Student)
                .OrderBy(g => g.Number)
                .ToListAsync();
        }
        public async Task<IEnumerable<Group>> GetInvitesForStudentAsync(Guid studentId)
        {
            return await context.Groups
                .AsNoTracking()
                .Where(g => g.IsActive && g.Members.Any(m => m.StudentId == studentId && m.Status == GroupMemberStatus.Invited))
                .Include(g => g.Event)
                .Include(g => g.Leader)
                .Include(g => g.Members)
                .ThenInclude(m => m.Student)
                .ToListAsync();
        }
        public async Task AddMemberAsync(GroupMember member)
        {
            await context.GroupMembers.AddAsync(member);
            await context.SaveChangesAsync();
        }
        public async Task RemoveMemberAsync(GroupMember member)
        {
            context.GroupMembers.Remove(member);
            await context.SaveChangesAsync();
        }
        public async Task SetMemberStatusAsync(GroupMember member, GroupMemberStatus status)
        {
            member.Status = status;
            if (status == GroupMemberStatus.Accepted)
                member.JoinedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        public async Task DeclinePendingInvitesAsync(Guid studentId, Guid eventId, Guid exceptGroupId)
        {
            var invites = await context.GroupMembers
                .Where(m => m.StudentId == studentId && m.EventId == eventId && m.GroupId != exceptGroupId && m.Status == GroupMemberStatus.Invited)
                .ToListAsync();
            foreach (var invite in invites)
                invite.Status = GroupMemberStatus.Declined;
            await context.SaveChangesAsync();
        }
        public async Task<bool> IsStudentRegisteredAsync(Guid studentId, Guid eventId)
        {
            return await context.Events
                .Where(e => e.Id == eventId)
                .SelectMany(e => e.EventStudents)
                .AnyAsync(s => s.StudentId == studentId && s.Status == RegistrationStatus.Confirmed);
        }
        public async Task<bool> HasAcceptedGroupAsync(Guid studentId, Guid eventId)
        {
            return await context.GroupMembers
                .AnyAsync(m => m.StudentId == studentId && m.EventId == eventId && m.Status == GroupMemberStatus.Accepted);
        }
        public async Task<IEnumerable<Student>> GetStudentsAvailableForGroupAsync(Guid eventId)
        {
            return await context.Events
                .Where(e => e.Id == eventId)
                .SelectMany(e => e.EventStudents)
                .Where(s => s.Status == RegistrationStatus.Confirmed
                    && !context.GroupMembers.Any(m => m.StudentId == s.StudentId && m.EventId == eventId && m.Status == GroupMemberStatus.Accepted))
                .Select(s => s.Student)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }
        public async Task<Group> UpdateAsync(Group group)
        {
            context.Groups.Update(group);
            await context.SaveChangesAsync();
            return group;
        }
        public async Task DeleteAsync(Guid groupId)
        {
            var group = await GetByIdAsync(groupId);
            if (group != null)
            {
                group.IsActive = false;
            }
            await context.SaveChangesAsync();
        }
    }
    public interface IGroupRepository
    {
        Task<Group?> GetGroupForStudentInEventAsync(Guid studentId, Guid eventId);
        Task<Group?> GetByIdAsync(Guid groupId);
        Task<Group> AddAsync(Group group);
        Task<int> GetNextNumberAsync(Guid eventId);
        Task<IEnumerable<Group>> GetByEventAsync(Guid eventId);
        Task<IEnumerable<Group>> GetInvitesForStudentAsync(Guid studentId);
        Task AddMemberAsync(GroupMember member);
        Task RemoveMemberAsync(GroupMember member);
        Task SetMemberStatusAsync(GroupMember member, GroupMemberStatus status);
        Task DeclinePendingInvitesAsync(Guid studentId, Guid eventId, Guid exceptGroupId);
        Task<bool> IsStudentRegisteredAsync(Guid studentId, Guid eventId);
        Task<bool> HasAcceptedGroupAsync(Guid studentId, Guid eventId);
        Task<IEnumerable<Student>> GetStudentsAvailableForGroupAsync(Guid eventId);
        Task<Group> UpdateAsync(Group group);
        Task DeleteAsync(Guid groupId);
    }
}
