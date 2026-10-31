using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace API_Gestao_Eventos.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _dbConn;

        public CustomWebApplicationFactory()
        {
            _dbConn = new SqliteConnection($"DataSource=file:test_integration_{Guid.NewGuid():N}?mode=memory&cache=shared");
            _dbConn.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("DatabaseProvider", "None");

            builder.ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_dbConn);
                });

                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IEmailService)).ToList())
                    services.Remove(descriptor);

                services.AddSingleton<IEmailService, FakeEmailService>();

                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _dbConn.Dispose();
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Headers.ContainsKey("X-Anonymous"))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = Guid.NewGuid().ToString();
            if (Request.Headers.TryGetValue("X-Test-User-Id", out var userIdHeader)
                && !string.IsNullOrWhiteSpace(userIdHeader))
            {
                userId = userIdHeader.ToString();
            }

            var role = "Administrador";
            if (Request.Headers.TryGetValue("X-Test-User-Role", out var roleHeader)
                && !string.IsNullOrWhiteSpace(roleHeader))
            {
                role = roleHeader.ToString();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "Admin Tester"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// Substitui o SendGrid nos testes para não depender de API externa.
    /// </summary>
    public class FakeEmailService : IEmailService
    {
        public Task SendEmailAsync(EmailAddress to, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }
}
