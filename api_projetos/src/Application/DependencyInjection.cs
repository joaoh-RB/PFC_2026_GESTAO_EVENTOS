using API_Gestao_Eventos.src.Application.Services;

namespace API_Gestao_Eventos.src.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<AuthService>();
            services.AddScoped<CourseService>();
            services.AddScoped<InstitutionService>();
            return services;
        }
    }
}
