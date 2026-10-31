using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

namespace API_Gestao_Eventos.Tests.Integration
{
    /// <summary>
    /// Testes de integração do AuthController.
    /// Fluxo real: HttpClient -> HTTP -> AuthController -> AuthService -> UserRepository -> SQLite em memória.
    /// </summary>
    public class AuthControllerIntegrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        private const string BasePath = "/api/auth";
        private const string SenhaValida = "Senha@123";

        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthControllerIntegrationTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ---------- POST /api/auth/register ----------

        [Fact]
        public async Task Register_ComDadosValidos_DeveRetornar201EPersistirUsuarioNoBanco()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"aluno{ts}@teste.com";
            var request = new RegisterRequestDto
            {
                Name = "Aluno Integração",
                Email = email,
                Password = SenhaValida,
                InstitutionId = institutionId,
                CourseId = courseId,
                UniqueIdentifier = $"RGM{ts}"
            };

            var response = await _client.PostAsJsonAsync($"{BasePath}/register", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Contains("Cadastro realizado com sucesso", body.Message);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email);
            Assert.NotNull(persisted);
            Assert.Equal("Aluno Integração", persisted.Name);
            Assert.Equal(institutionId, persisted.InstitutionId);
            Assert.Equal(courseId, persisted.CourseId);
            Assert.Equal($"RGM{ts}", persisted.UniqueIdentifier);
            Assert.Equal(UserApprovalStatus.Pendente, persisted.ApprovalStatus);
        }

        [Fact]
        public async Task Register_ComEmailDuplicado_DeveRetornar400ComMensagem()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"duplicado{ts}@teste.com";
            await SeedStudentAsync(email, institutionId, courseId, $"DUP{ts}");

            var request = new RegisterRequestDto
            {
                Name = "Outro Aluno",
                Email = email,
                Password = SenhaValida,
                InstitutionId = institutionId,
                CourseId = courseId,
                UniqueIdentifier = $"OUTRO{ts}"
            };

            var response = await _client.PostAsJsonAsync($"{BasePath}/register", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("E-mail já cadastrado.", content);
        }

        // ---------- POST /api/auth/login ----------

        [Fact]
        public async Task Login_ComUsuarioAprovado_DeveRetornar200ComUsuario()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"login{ts}@teste.com";
            var student = await SeedStudentAsync(
                email, institutionId, courseId, $"LGN{ts}",
                approvalStatus: UserApprovalStatus.Aprovado);

            var response = await _client.PostAsJsonAsync($"{BasePath}/login", new LoginRequestDto
            {
                Email = email,
                Password = SenhaValida
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var user = doc.RootElement.GetProperty("user");
            Assert.Equal(student.Id, user.GetProperty("id").GetGuid());
            Assert.Equal(email, user.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Login_ComCredenciaisInvalidas_DeveRetornar401ComMensagem()
        {
            var response = await _client.PostAsJsonAsync($"{BasePath}/login", new LoginRequestDto
            {
                Email = $"inexistente{DateTime.UtcNow.Ticks}@teste.com",
                Password = "Qualquer@1"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Credenciais inválidas.", content);
        }

        // ---------- POST /api/auth/logout ----------

        [Fact]
        public async Task Logout_DeveRetornar200ComMensagem()
        {
            var response = await _client.PostAsync($"{BasePath}/logout", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Logout realizado com sucesso.", content);
        }

        // ---------- GET /api/auth/me ----------

        [Fact]
        public async Task GetCurrentUser_ComUsuarioAutenticado_DeveRetornar200ComDados()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var student = await SeedStudentAsync(
                $"me{ts}@teste.com", institutionId, courseId, $"ME{ts}",
                approvalStatus: UserApprovalStatus.Aprovado);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/me");
            request.Headers.Add("X-Test-UserId", student.Id.ToString());
            request.Headers.Add("X-Test-User-Role", "Aluno");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<UserResponseDto>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal(student.Id, body.Id);
            Assert.Equal(student.Email, body.Email);
            Assert.Equal(student.Name, body.Name);
        }

        // ---------- POST /api/auth/2fa/setup ----------

        [Fact]
        public async Task Setup2FA_ComUsuarioAutenticado_DeveRetornar200ComChaveEQrCode()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var student = await SeedStudentAsync(
                $"setup2fa{ts}@teste.com", institutionId, courseId, $"S2FA{ts}",
                approvalStatus: UserApprovalStatus.Aprovado);

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/2fa/setup");
            request.Headers.Add("X-Test-UserId", student.Id.ToString());
            request.Headers.Add("X-Test-User-Role", "Aluno");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<TwoFactorCreateResponseDto>(_jsonOptions);
            Assert.NotNull(body);
            Assert.False(string.IsNullOrWhiteSpace(body.SecretKey));
            Assert.Contains("otpauth://totp/", body.QrCodeUrl);
            Assert.Contains(student.Email, Uri.UnescapeDataString(body.QrCodeUrl));
        }

        // ---------- POST /api/auth/2fa/enable ----------

        [Fact]
        public async Task Enable2FA_ComCodigoValido_DeveRetornar200()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var student = await SeedStudentAsync(
                $"enable2fa{ts}@teste.com", institutionId, courseId, $"E2FA{ts}",
                approvalStatus: UserApprovalStatus.Aprovado);

            using var setupRequest = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/2fa/setup");
            setupRequest.Headers.Add("X-Test-UserId", student.Id.ToString());
            var setupResponse = await _client.SendAsync(setupRequest);
            setupResponse.EnsureSuccessStatusCode();
            var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorCreateResponseDto>(_jsonOptions);
            Assert.NotNull(setup);

            var code = new Totp(Base32Encoding.ToBytes(setup.SecretKey)).ComputeTotp();

            using var enableRequest = new HttpRequestMessage(HttpMethod.Post, $"{BasePath}/2fa/enable")
            {
                Content = JsonContent.Create(new TwoFactorEnableRequestDto { Code = code })
            };
            enableRequest.Headers.Add("X-Test-UserId", student.Id.ToString());

            var response = await _client.SendAsync(enableRequest);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Autenticação 2FA ativada com sucesso.", content);
        }

        // ---------- POST /api/auth/change-initial-password ----------

        [Fact]
        public async Task ChangeInitialPassword_ComDadosValidos_DeveRetornar200()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"trocasenha{ts}@teste.com";
            await SeedStudentAsync(
                email, institutionId, courseId, $"CHG{ts}",
                approvalStatus: UserApprovalStatus.Aprovado,
                passwordChangeRequired: true);

            var response = await _client.PostAsJsonAsync($"{BasePath}/change-initial-password", new ChangeInitialPasswordDto
            {
                Email = email,
                CurrentPassword = SenhaValida,
                NewPassword = "NovaSenha@456",
                ConfirmPassword = "NovaSenha@456"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Senha definida com sucesso!", content);
        }

        // ---------- POST /api/auth/forgot-password ----------

        [Fact]
        public async Task ForgotPassword_ComEmailCadastrado_DeveRetornar200()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"forgot{ts}@teste.com";
            await SeedStudentAsync(
                email, institutionId, courseId, $"FGT{ts}",
                approvalStatus: UserApprovalStatus.Aprovado);

            var response = await _client.PostAsJsonAsync($"{BasePath}/forgot-password", new ForgotPasswordRequestDto
            {
                Email = email
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Solicitação de redefinição de senha enviada com sucesso", content);
        }

        // ---------- POST /api/auth/reset-password ----------

        [Fact]
        public async Task ResetPassword_ComTokenValido_DeveRetornar200()
        {
            var (institutionId, courseId) = await SeedInstitutionAndCourseAsync();
            var ts = DateTime.UtcNow.Ticks;
            var email = $"reset{ts}@teste.com";
            const string resetToken = "TokenReset@1";
            await SeedStudentAsync(
                email, institutionId, courseId, $"RST{ts}",
                approvalStatus: UserApprovalStatus.Aprovado,
                passwordResetToken: resetToken);

            var response = await _client.PostAsJsonAsync($"{BasePath}/reset-password", new ResetPasswordRequestDto
            {
                Email = email,
                Token = resetToken,
                NewPassword = "Resetada@789",
                ConfirmPassword = "Resetada@789"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Senha redefinida com sucesso!", content);
        }

        // ---------- Helpers ----------

        private async Task<(Guid InstitutionId, Guid CourseId)> SeedInstitutionAndCourseAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ts = DateTime.UtcNow.Ticks;
            var institution = new Institution { Name = $"Instituição Auth {ts}", IsActive = true };
            var course = new Course
            {
                Name = $"Curso Auth {ts}",
                Institution = institution,
                InstitutionId = institution.Id,
                IsActive = true
            };
            await db.Institutions.AddAsync(institution);
            await db.Courses.AddAsync(course);
            await db.SaveChangesAsync();
            return (institution.Id, course.Id);
        }

        private async Task<Student> SeedStudentAsync(
            string email,
            Guid institutionId,
            Guid courseId,
            string uniqueIdentifier,
            UserApprovalStatus approvalStatus = UserApprovalStatus.Pendente,
            bool passwordChangeRequired = false,
            string? passwordResetToken = null)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IHasher>();

            var student = new Student
            {
                Name = "Aluno Teste",
                Email = email,
                PasswordHash = hasher.HashPassword(SenhaValida),
                UniqueIdentifier = uniqueIdentifier,
                InstitutionId = institutionId,
                CourseId = courseId,
                IsActive = true,
                ApprovalStatus = approvalStatus,
                IsPasswordChangeRequired = passwordChangeRequired
            };

            if (!string.IsNullOrEmpty(passwordResetToken))
            {
                student.PasswordResetTokenHash = hasher.HashPassword(passwordResetToken);
                student.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
            }

            await db.Students.AddAsync(student);
            await db.SaveChangesAsync();
            return student;
        }
    }
}
