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
    }
}
