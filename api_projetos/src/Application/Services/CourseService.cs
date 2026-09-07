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
        public async Task<Guid> CreateCourseAsync(
            CreateCourseDto request,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isInstitutionAdmin)
        {
            var isAllowed = isAdmin || (isInstitutionAdmin && callerInstitutionId == request.InstitutionId);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para cadastrar cursos nesta instituição.");

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
        public async Task<CourseDetailDto?> GetByIdAsync(
            Guid id,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isInstitutionAdmin)
        {
            var course = await _courseRepository.GetByIdAsync(id);
            if (course == null) return null;

            var isAllowed = isAdmin || (isInstitutionAdmin && callerInstitutionId == course.InstitutionId);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para consultar este curso.");

            return new CourseDetailDto
            {
                Id = course.Id,
                Name = course.Name,
                InstitutionId = course.InstitutionId,
                IsActive = course.IsActive,
                CreatedAt = course.CreatedAt
            };
        }
        public async Task UpdateCourseAsync(
            Guid id,
            UpdateCourseDto request,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isInstitutionAdmin)
        {
            var course = await _courseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Curso não encontrado.");

            var isAllowed = isAdmin || (isInstitutionAdmin && callerInstitutionId == course.InstitutionId);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para editar este curso.");

            var nameExists = await _courseRepository.ExistsByNameExceptAsync(request.Name, course.InstitutionId, id);
            if (nameExists)
                throw new InvalidOperationException("Já existe outro curso com este nome nesta instituição.");

            course.Name = request.Name;
            await _courseRepository.UpdateAsync(course);
        }
        public async Task DeleteCourseAsync(
            Guid id,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isInstitutionAdmin)
        {
            var course = await _courseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Curso não encontrado.");

            var isAllowed = isAdmin || (isInstitutionAdmin && callerInstitutionId == course.InstitutionId);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para excluir este curso.");

            if (await _courseRepository.HasDependenciesAsync(id))
                throw new InvalidOperationException("Não é possível excluir: existem alunos, professores ou eventos vinculados a este curso.");

            await _courseRepository.DeleteAsync(course);
        }
    }
}