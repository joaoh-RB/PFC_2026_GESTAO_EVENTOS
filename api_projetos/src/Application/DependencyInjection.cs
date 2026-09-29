using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Application.Validators.Auth;
using API_Gestao_Eventos.src.Application.Validators.Course;
using API_Gestao_Eventos.src.Application.Validators.Event;
using API_Gestao_Eventos.src.Application.Validators.Institution;
using API_Gestao_Eventos.src.Application.Validators.User;
using FluentValidation;
using FluentValidation.AspNetCore;
namespace API_Gestao_Eventos.src.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<AuthService>();
            services.AddScoped<CourseService>();
            services.AddScoped<InstitutionService>();
            services.AddScoped<EventService>();
            services.AddScoped<UserService>();
            services.AddScoped<LegalDocumentService>();
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateEventValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateInstitutionMemberValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateStudentValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateStudentValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateInstitutionMemberValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateCourseValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateCourseValidator>();
            services.AddValidatorsFromAssemblyContaining<CreateInstitutionValidator>();
            services.AddValidatorsFromAssemblyContaining<UpdateInstitutionValidator>();
            services.AddValidatorsFromAssemblyContaining<ChangeInitialPasswordValidator>();
            services.AddValidatorsFromAssemblyContaining<ResetPasswordValidator>();
            return services;
        }
    }
}
