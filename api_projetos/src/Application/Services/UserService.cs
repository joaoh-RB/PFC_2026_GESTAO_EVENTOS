using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
namespace API_Gestao_Eventos.src.Application.Services
{
    public class UserService(
        UserRepository userRepository,
        InstitutionRepository institutionRepository,
        CourseRepository courseRepository,
        IHasher passwordHasher)
    {
        public async Task<IEnumerable<UserManagementResponseDto>> GetAllAsync()
        {
            var users = await userRepository.GetAllStudentsAsync();
            return users.Select(ToResponse);
        }
        #region Membros instituição

        public async Task<InstitutionMemberResponseDto> CreateInstitutionMember(CreateInstitutionMemberRequestDto request)
        {
            User user;
            if (request.UserRole == UserRole.Professor)
            {
                var courses = (await courseRepository.GetByInstitutionAsync(request.InstitutionId)).Where(w => request.Courses!.Contains(w.Id)).ToList();
                if (courses.Count != request.Courses!.Count)
                {
                    throw new InvalidOperationException("Nem todos os cursos cadastrados são permitidos.");
                }
                user = new Teacher()
                {
                    Courses = courses
                };
            }
            else
                user = new AcademicDepartment();
            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim();
            user.PasswordHash = passwordHasher.HashPassword(request.Password);
            user.InstitutionId = request.InstitutionId;
            if (await userRepository.ExistsByEmailAsync(request.Email))
                throw new InvalidOperationException("E-mail já cadastrado.");
            await userRepository.AddAsync(user);
            return ToInstitutionMemberResponse(await userRepository.GetByIdAsync(user.Id));
        }
        public async Task<PagedResponseDto<InstitutionMemberResponseDto>> GetInstitutionMembersPagedAsync(InstitutionMemberFilterDto filter)
        {
            var (users, totalCount) = await userRepository.GetFilteredInstitutionMembersAsync(filter);

            var items = users.Select(ToInstitutionMemberResponse);

            return new PagedResponseDto<InstitutionMemberResponseDto>
            {
                Items = items,
                TotalItems = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }
        public async Task UpdateInstitutionMemberAsync(Guid id, UpdateInstitutionMemberRequestDto request)
        {
            var user = await userRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Usuário não encontrado");
            if (user.Role != UserRole.Professor && user.Role != UserRole.Secretaria)
                throw new InvalidOperationException("Usuário não é um membro da instituição.");
            if (await userRepository.ExistsByEmailExceptAsync(request.Email, id))
                throw new InvalidOperationException("E-mail já cadastrado.");
            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = passwordHasher.HashPassword(request.Password);
            if (user is Teacher t)
            {
                var courses = (await courseRepository.GetByInstitutionAsync(user.InstitutionId!.Value)).Where(w => request.Courses!.Contains(w.Id)).ToList();
                if (courses.Count != request.Courses!.Count)
                {
                    throw new InvalidOperationException("Nem todos os cursos cadastrados são permitidos.");
                }
                t.Courses.Clear();
                foreach (var course in courses)
                {
                    t.Courses.Add(course);
                }
            }
            await userRepository.UpdateAsync(user);
        }
        private static InstitutionMemberResponseDto ToInstitutionMemberResponse(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            InstitutionId = user.InstitutionId ?? Guid.Empty,
            UserRole = user.Role.ToString(),
            UserRoleId = (int)user.Role,
            InstitutionName = user.Institution!.Name,
            CoursesIds = (user is Teacher t ? t.Courses.Select(s => s.Id) : null),
            CoursesNames = (user is Teacher teacher ? teacher.Courses.Select(s => s.Name) : null),
            IsActive = user.IsActive
        };
        #endregion
        public async Task<UserManagementResponseDto> GetByIdAsync(Guid id)
        {
            return ToResponse(await GetStudentAsync(id));
        }
        public async Task<UserManagementResponseDto> CreateStudentAsync(RegisterRequestDto request)
        {
            await ValidateReferencesAsync(request.InstitutionId, request.CourseId);
            if (await userRepository.ExistsByEmailAsync(request.Email))
                throw new InvalidOperationException("E-mail já cadastrado.");
            if (await userRepository.ExistsByUniqueIdentifierAndInstitutionAsync(request.UniqueIdentifier, request.InstitutionId))
                throw new InvalidOperationException("RGM/Matrícula já cadastrada para esta instituição.");
            var student = new Student
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                PasswordHash = passwordHasher.HashPassword(request.Password),
                UniqueIdentifier = request.UniqueIdentifier.Trim(),
                InstitutionId = request.InstitutionId,
                CourseId = request.CourseId
            };
            await userRepository.AddAsync(student);
            return ToResponse(student);
        }
        public async Task<UserManagementResponseDto> UpdateAsync(Guid id, UpdateUserRequestDto request)
        {
            var user = await GetStudentAsync(id);
            await ValidateReferencesAsync(request.InstitutionId, request.CourseId);
            if (await userRepository.ExistsByEmailExceptAsync(request.Email, id))
                throw new InvalidOperationException("E-mail já cadastrado.");
            if (await userRepository.ExistsByUniqueIdentifierAndInstitutionExceptAsync(request.UniqueIdentifier, request.InstitutionId, id))
                throw new InvalidOperationException("RGM/Matrícula já cadastrada para esta instituição.");
            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim();
            user.UniqueIdentifier = request.UniqueIdentifier.Trim();
            user.InstitutionId = request.InstitutionId;
            user.CourseId = request.CourseId;
            user.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = passwordHasher.HashPassword(request.Password);
            await userRepository.UpdateAsync(user);
            return ToResponse(user);
        }
        public async Task SetApprovalAsync(Guid id, UserApprovalStatus status, Guid approverId)
        {
            if (status == UserApprovalStatus.Pendente)
                throw new InvalidOperationException("Use apenas aprovação ou reprovação.");
            if (id == approverId)
                throw new InvalidOperationException("Você não pode analisar o próprio cadastro.");
            var user = await GetStudentAsync(id);
            user.ApprovalStatus = status;
            user.ApprovedByUserId = approverId;
            user.ApprovedDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);
        }
        public async Task SetActiveAsync(Guid id, bool isActive)
        {
            var user = await userRepository.GetByIdAsync(id);
            user!.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);
        }
        private async Task<Student> GetStudentAsync(Guid id) =>
            await userRepository.GetStudentByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");
        private async Task ValidateReferencesAsync(Guid institutionId, Guid courseId)
        {
            if (!(await institutionRepository.GetAllAsync()).Any(i => i.Id == institutionId))
                throw new InvalidOperationException("Instituição inválida.");
            if (!(await courseRepository.GetAllAsync()).Any(c => c.Id == courseId))
                throw new InvalidOperationException("Curso inválido.");
        }
        private static UserManagementResponseDto ToResponse(Student user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            UniqueIdentifier = user.UniqueIdentifier ?? string.Empty,
            InstitutionId = user.InstitutionId ?? Guid.Empty,
            CourseId = user.CourseId,
            Role = user.Role,
            IsActive = user.IsActive,
            ApprovalStatus = user.ApprovalStatus,
            ApprovedByUserId = user.ApprovedByUserId,
            ApprovedByUserName = user.ApprovedByUser?.Name,
            ApprovedDate = user.ApprovedDate,
            CreatedAt = user.CreatedAt
        };
    }
}