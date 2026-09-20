namespace API_Gestao_Eventos.src.Application.DTO.Course
{
    public class CourseManagementItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}