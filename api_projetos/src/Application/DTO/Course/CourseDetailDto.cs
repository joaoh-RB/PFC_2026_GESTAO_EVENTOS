namespace API_Gestao_Eventos.src.Application.DTO.Course
{
    public class CourseDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid InstitutionId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}