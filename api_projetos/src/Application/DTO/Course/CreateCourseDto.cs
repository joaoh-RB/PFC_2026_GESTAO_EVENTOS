namespace API_Gestao_Eventos.src.Application.DTO.Course
{
    public class CreateCourseDto
    {
        public required string Name { get; set; }
        public required Guid InstitutionId { get; set; }
    }
}
