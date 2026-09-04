using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Application.Validators.Auth;
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
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
            return services;
        }
    }
}
