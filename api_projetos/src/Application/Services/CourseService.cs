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

            return courses
                .Select(i => new SelectItemDto
                {
                    Id = i.Id,
                    Name = i.Id.ToString()
                });
        }
    }
}
