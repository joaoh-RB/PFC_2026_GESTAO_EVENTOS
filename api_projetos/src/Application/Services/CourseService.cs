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
<<<<<<< HEAD
            return courses.Select(i => new SelectItemDto
            {
                Id = i.Id,
                Name = i.Name 
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
=======

            return courses
                .Select(i => new SelectItemDto
                {
                    Value = i.Id.ToString(),
                    Label = i.Id.ToString()
                });
>>>>>>> 7b0c935aabc5bad94f5fb9dba2e4cece0a6b6072
        }
    }
}
