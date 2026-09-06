using API_Gestao_Eventos.src.Application.DTO.Course;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class CourseService(CourseRepository courseRepository)
    {
        private readonly CourseRepository _courseRepository = courseRepository;
        public async Task<IEnumerable<SelectItemDto>> GetCoursesForSelectAsync()
        {
            var courses = await _courseRepository.GetAllAsync();
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
    }
}