using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Infrastructure.Data.Context
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Administrator> Administrators => Set<Administrator>();
        public DbSet<AcademicDepartment> AcademicDepartments => Set<AcademicDepartment>();
        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
        public DbSet<TermAcceptance> TermAcceptances => Set<TermAcceptance>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        public DbSet<EventAddedToCalendar> EventAddedToCalendars => Set<EventAddedToCalendar>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = new List<AuditLog>();

            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            var userId = AuditHelper.GetUserIdFromHttpContext(_httpContextAccessor);

            foreach (var entry in entries)
            {
                var audit = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId
                };

                var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();
                audit.EntityId = pk;

                if (entry.State == EntityState.Added)
                {
                    audit.OperationType = AuditOperationType.Insert;
                    var newValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.CurrentValues.Properties)
                    {
                        newValues[prop.Name] = entry.CurrentValues[prop.Name];
                    }
                    audit.NewValues = AuditHelper.SerializeObject(newValues);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    audit.OperationType = AuditOperationType.Delete;
                    var oldValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.OriginalValues.Properties)
                    {
                        oldValues[prop.Name] = entry.OriginalValues[prop.Name];
                    }
                    audit.OldValues = AuditHelper.SerializeObject(oldValues);
                }
                else if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();
                    var changedProps = new List<string>();

                    foreach (var prop in entry.OriginalValues.Properties)
                    {
                        var original = entry.OriginalValues[prop.Name];
                        var current = entry.CurrentValues[prop.Name];
                        if (!Equals(original, current))
                        {
                            oldValues[prop.Name] = original;
                            newValues[prop.Name] = current;
                            changedProps.Add(prop.Name);
                        }
                    }

                    if (!changedProps.Any())
                        continue;

                    audit.OperationType = AuditOperationType.Update;
                    audit.OldValues = AuditHelper.SerializeObject(oldValues);
                    audit.NewValues = AuditHelper.SerializeObject(newValues);
                    audit.ChangedProperties = string.Join(',', changedProps);
                }

                auditEntries.Add(audit);
            }

            if (auditEntries.Any())
            {
                foreach (var a in auditEntries)
                {
                    AuditLogs.Add(a);
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public async Task LogAuthEventAsync(AuditOperationType operationType, string? description = null, Guid? explicitUserId = null)
        {
            var audit = new AuditLog
            {
                OperationType = operationType,
                Description = description,
                UserId = explicitUserId ?? AuditHelper.GetUserIdFromHttpContext(_httpContextAccessor),
                EntityName = "Auth",
                Timestamp = DateTime.UtcNow
            };

            AuditLogs.Add(audit);
            await base.SaveChangesAsync();
        }
    }
}