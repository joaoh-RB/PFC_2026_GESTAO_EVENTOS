using API_Gestao_Eventos.src.Domain.Enums;
namespace API_Gestao_Eventos.src.Application.DTO.User
{
    public class UserManagementResponseDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string UniqueIdentifier { get; set; }
        public Guid InstitutionId { get; set; }
        public Guid CourseId { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public UserApprovalStatus ApprovalStatus { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class UpdateUserRequestDto
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string UniqueIdentifier { get; set; }
        public required Guid InstitutionId { get; set; }
        public required Guid CourseId { get; set; }
        public string? Password { get; set; }
    }
}