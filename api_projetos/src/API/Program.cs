using API_Gestao_Eventos.src.Application;
using API_Gestao_Eventos.src.Infrastructure;
using API_Gestao_Eventos.src.Infrastructure.Data.Seed;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var frontendUrl = builder.Configuration.GetSection("FrontendInfo")["BaseUrl"] ?? "https://localhost:3000";
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed local (Development): limpa dados de teste e recria massa válida via entidades C#.
// Preserva admin contapfcgestaoeventos@gmail.com e LegalDocuments.
await DemoDataSeeder.RunIfEnabledAsync(app);

app.Run();

public partial class Program { }