using API_Gestao_Eventos.src.Domain.Entities;
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
                .Where(g => g.EventId == eventId && g.Members.Any(s => s.StudentId == studentId))
                .Include(g => g.Documents)
                .ThenInclude(g => g.Sender)
                .FirstOrDefaultAsync();
        }
        public async Task<Group?> GetByIdAsync(Guid groupId)
        {
            return await context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId);
        }
        public async Task<Group> AddAsync(Group group)
        {
            await context.Groups.AddAsync(group);
            await context.SaveChangesAsync();
            return group;
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
        Task<Group> UpdateAsync(Group group);
        Task DeleteAsync(Guid groupId);
    }
}
