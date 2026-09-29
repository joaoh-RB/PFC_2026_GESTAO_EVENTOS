using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class CourseService(CourseRepository courseRepository)
    {
        private readonly CourseRepository _courseRepository = courseRepository;
        public async Task<IEnumerable<SelectItemDto>> GetCoursesForSelectAsync(Guid? institutionId)
        {
            if (!institutionId.HasValue || institutionId == Guid.Empty)
            {
                return new List<SelectItemDto>();
            }
            var courses = await _courseRepository.GetByInstitutionAsync(institutionId.Value);
            return courses.Select(i => new SelectItemDto
            {
                Value = i.Id.ToString(),
                Label = i.Name
            });
        }
        public async Task<Guid> CreateCourseAsync(CreateCourseDto request)
        {
            var nameExists = await _courseRepository.ExistsByNameAsync(request.Name, request.InstitutionId);
            if (nameExists)
                throw new InvalidOperationException("Curso já cadastrado nesta instituição.");
            var course = new Domain.Entities.Course
            {
                Name = request.Name,
                InstitutionId = request.InstitutionId
            };
            await _courseRepository.AddAsync(course);
            return course.Id;
        }
        public async Task<CourseDetailDto?> GetByIdAsync(Guid id)
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null) return null;

            return new CourseDetailDto
            {
                Id = course.Id,
                Name = course.Name,
                InstitutionId = course.InstitutionId,
                IsActive = course.IsActive,
                CreatedAt = course.CreatedAt
            };
        }
        public async Task UpdateCourseAsync(Guid id, UpdateCourseDto request)
        {
            var course = await _courseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Curso não encontrado.");

            var nameExists = await _courseRepository.ExistsByNameExceptAsync(request.Name, course.InstitutionId, id);
            if (nameExists)
                throw new InvalidOperationException("Já existe outro curso com este nome nesta instituição.");

            course.Name = request.Name;
            await _courseRepository.UpdateAsync(course);
        }
        public async Task<IEnumerable<CourseManagementItemDto>> GetForManagementAsync(Guid institutionId)
        {
            var courses = await _courseRepository.GetByInstitutionForManagementAsync(institutionId);
            return courses.Select(c => new CourseManagementItemDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive
            });
        }
        public async Task SetCourseActiveAsync(Guid id, bool isActive)
        {
            var course = await _courseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Curso não encontrado.");

            if (!isActive && await _courseRepository.HasDependenciesAsync(id))
                throw new InvalidOperationException("Não é possível inativar: existem alunos, professores ou eventos vinculados a este curso.");

            await _courseRepository.SetActiveAsync(course, isActive);
        }
    }
}