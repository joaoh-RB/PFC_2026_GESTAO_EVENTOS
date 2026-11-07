using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Email;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using SendGrid.Helpers.Mail;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IHasher> _hasherMock;
        private readonly Mock<IJwtTokenGenerator> _jwtMock;
        private readonly Mock<IGoogleAuthService> _googleAuthMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly AppDbContext _dbContext;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _hasherMock = new Mock<IHasher>();
            _jwtMock = new Mock<IJwtTokenGenerator>();
            _googleAuthMock = new Mock<IGoogleAuthService>();
            _emailServiceMock = new Mock<IEmailService>();
            _userRepositoryMock = new Mock<IUserRepository>();

            var tempRoot = Path.Combine(Path.GetTempPath(), "symplosio_tests_env");
            var templatesDir = Path.Combine(tempRoot, "Frontend", "public", "email-templates");
            var apiDir = Path.Combine(tempRoot, "api_projetos");
            Directory.CreateDirectory(templatesDir);
            Directory.CreateDirectory(apiDir);
            var templateFile = Path.Combine(templatesDir, "notificacao-generica.html");
            if (!File.Exists(templateFile))
                File.WriteAllText(templateFile, "<html>{{titulo}} {{nome}} {{mensagem}} {{link_acao}} {{texto_botao}}</html>");

            var hostEnvMock = new Mock<IHostEnvironment>();
            hostEnvMock.Setup(h => h.ContentRootPath).Returns(apiDir);
            var renderer = new EmailTemplateRenderer(hostEnvMock.Object);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["FrontendInfo:BaseUrl"] = "http://localhost:3000" })
                .Build();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new AppDbContext(options, null);

            _authService = new AuthService(
                _hasherMock.Object,
                _jwtMock.Object,
                _googleAuthMock.Object,
                _emailServiceMock.Object,
                renderer,
                config,
                _dbContext,
                _userRepositoryMock.Object);
        }

        private static RegisterRequestDto SeedRegisterDto() => new()
        {
            Name = "Aluno Novo",
            Email = "aluno@teste.com",
            Password = "Senha@123",
            InstitutionId = Guid.NewGuid(),
            CourseId = Guid.NewGuid(),
            UniqueIdentifier = "RGM12345"
        };

        private static Student SeedStudent(
            bool isActive = true,
            UserApprovalStatus approvalStatus = UserApprovalStatus.Aprovado,
            bool isPasswordChangeRequired = false,
            bool twoFactorEnabled = false) => new()
            {
                Id = Guid.NewGuid(),
                Name = "Aluno Teste",
                Email = "aluno@teste.com",
                PasswordHash = "hash-senha",
                IsActive = isActive,
                ApprovalStatus = approvalStatus,
                IsPasswordChangeRequired = isPasswordChangeRequired,
                TwoFactorEnabled = twoFactorEnabled,
                TwoFactorSecretKey = "SECRET123",
                InstitutionId = Guid.NewGuid(),
                Institution = new Institution { Id = Guid.NewGuid(), Name = "Instituição Teste" }
            };

        [Fact]
        public async Task RegisterStudentAsync_QuandoEmailJaExistir_DeveLancarInvalidOperationException()
        {
            var dto = SeedRegisterDto();
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterStudentAsync(dto));

            Assert.Equal("E-mail já cadastrado.", exception.Message);
        }

        [Fact]
        public async Task RegisterStudentAsync_QuandoRgmJaExistirNaInstituicao_DeveLancarInvalidOperationException()
        {
            var dto = SeedRegisterDto();
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUniqueIdentifierAndInstitutionAsync(dto.UniqueIdentifier, dto.InstitutionId)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterStudentAsync(dto));

            Assert.Equal("RGM/Matrícula já cadastrada para esta instituição.", exception.Message);
        }

        [Fact]
        public async Task RegisterStudentAsync_QuandoDadosValidos_DeveCadastrarAlunoERetornarMensagem()
        {
            var dto = SeedRegisterDto();
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUniqueIdentifierAndInstitutionAsync(dto.UniqueIdentifier, dto.InstitutionId)).ReturnsAsync(false);
            _hasherMock.Setup(h => h.HashPassword(dto.Password)).Returns("hashed");

            var response = await _authService.RegisterStudentAsync(dto);

            Assert.False(response.RequiresTwoFactor);
            Assert.False(response.RequiresPasswordChange);
            Assert.Contains("Cadastro realizado com sucesso", response.Message);
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<Student>(s => s.Email == dto.Email && s.PasswordHash == "hashed")), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_QuandoCredenciaisInvalidas_DeveLancarUnauthorizedAccessException()
        {
            var user = SeedStudent();
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("errada", user.PasswordHash)).Returns(false);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "errada" }));

            Assert.Equal("Credenciais inválidas.", exception.Message);
        }

        [Fact]
        public async Task LoginAsync_QuandoUsuarioInativoOuNaoAprovado_DeveLancarUnauthorizedAccessException()
        {
            var inactiveUser = SeedStudent(isActive: false);
            var pendingUser = SeedStudent(approvalStatus: UserApprovalStatus.Pendente);
            var rejectedUser = SeedStudent(approvalStatus: UserApprovalStatus.Reprovado);

            _hasherMock.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _userRepositoryMock.Setup(r => r.GetByEmailAsync("inativo@teste.com")).ReturnsAsync(inactiveUser);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(new LoginRequestDto { Email = "inativo@teste.com", Password = "123" }));

            _userRepositoryMock.Setup(r => r.GetByEmailAsync("pendente@teste.com")).ReturnsAsync(pendingUser);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(new LoginRequestDto { Email = "pendente@teste.com", Password = "123" }));

            _userRepositoryMock.Setup(r => r.GetByEmailAsync("reprovado@teste.com")).ReturnsAsync(rejectedUser);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(new LoginRequestDto { Email = "reprovado@teste.com", Password = "123" }));
        }

        [Fact]
        public async Task LoginAsync_QuandoExigirTrocaDeSenhaInicial_DeveRetornarRequiresPasswordChangeTrue()
        {
            var user = SeedStudent(isPasswordChangeRequired: true);
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("Senha@123", user.PasswordHash)).Returns(true);

            var result = await _authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Senha@123" });

            Assert.True(result.RequiresPasswordChange);
            Assert.Null(result.Token);
        }

        [Fact]
        public async Task LoginAsync_QuandoTwoFactorAtivoSemCodigoOuComCodigoInvalido_DeveTratar2FA()
        {
            var user = SeedStudent(twoFactorEnabled: true);
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("Senha@123", user.PasswordHash)).Returns(true);

            var missingCodeResponse = await _authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Senha@123" });
            Assert.True(missingCodeResponse.RequiresTwoFactor);

            _googleAuthMock.Setup(g => g.ValidateTwoFactorCode(user.TwoFactorSecretKey!, "000000")).Returns(false);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Senha@123", TwoFactorCode = "000000" }));
        }

        [Fact]
        public async Task LoginAsync_QuandoLoginValidoCom2FA_DeveGerarTokenERetornarUsuario()
        {
            var user = SeedStudent(twoFactorEnabled: true);
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword("Senha@123", user.PasswordHash)).Returns(true);
            _googleAuthMock.Setup(g => g.ValidateTwoFactorCode(user.TwoFactorSecretKey!, "123456")).Returns(true);
            _jwtMock.Setup(j => j.GenerateToken(user)).Returns("jwt-token-gerado");

            var result = await _authService.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Senha@123", TwoFactorCode = "123456" });

            Assert.Equal("jwt-token-gerado", result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(user.Id, result.User.Id);
        }

        [Fact]
        public async Task SetupTwoFactorAsync_EEnableTwoFactorAsync_DevemConfigurarEAtivar2FA()
        {
            var user = SeedStudent();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _googleAuthMock.Setup(g => g.GenerateSecretKey()).Returns("NEWSECRET");
            _googleAuthMock.Setup(g => g.GenerateQrCodeUri(user.Email, "NEWSECRET")).Returns("otpauth://totp/teste");
            _googleAuthMock.Setup(g => g.ValidateTwoFactorCode("NEWSECRET", "654321")).Returns(true);
            var result = await _authService.SetupTwoFactorAsync(user.Id);
            Assert.Equal("otpauth://totp/teste", result.QrCodeUrl);
            Assert.Equal("NEWSECRET", result.SecretKey);
        }
        [Fact]
        public async Task SetupTwoFactorAsync_QuandoUsuarioNaoExistir_DevemLancarNullReferenceException()
        {
            var user = SeedStudent();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync((Student)null!);
            _googleAuthMock.Setup(g => g.GenerateSecretKey()).Returns("NEWSECRET");
            _googleAuthMock.Setup(g => g.GenerateQrCodeUri(user.Email, "NEWSECRET")).Returns("otpauth://totp/teste");
            _googleAuthMock.Setup(g => g.ValidateTwoFactorCode("NEWSECRET", "654321")).Returns(true);

            await Assert.ThrowsAsync<NullReferenceException>(() => _authService.SetupTwoFactorAsync(user.Id));
        }

        [Fact]
        public async Task ChangeInitialPasswordAsync_QuandoDadosValidos_DeveAlterarSenhaEGerarToken()
        {
            var user = SeedStudent(isPasswordChangeRequired: true);
            var dto = new ChangeInitialPasswordDto
            {
                Email = user.Email,
                CurrentPassword = "TempPassword1!",
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!"
            };
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword(dto.CurrentPassword, user.PasswordHash)).Returns(true);
            _hasherMock.Setup(h => h.VerifyPassword(dto.NewPassword, user.PasswordHash)).Returns(false);
            _hasherMock.Setup(h => h.HashPassword(dto.NewPassword)).Returns("new-hash");
            _jwtMock.Setup(j => j.GenerateToken(user)).Returns("novo-jwt");

            var result = await _authService.ChangeInitialPasswordAsync(dto);

            Assert.False(user.IsPasswordChangeRequired);
            Assert.Equal("new-hash", user.PasswordHash);
            Assert.Equal("novo-jwt", result.Token);
        }
        [Fact]
        public async Task ChangeInitialPasswordAsync_QuandoUsuarioInativo_DeveLancarUnauthorizedAccessExceptionComMensagem()
        {
            var user = SeedStudent(isActive: false);
            var dto = new ChangeInitialPasswordDto
            {
                Email = user.Email,
                CurrentPassword = "TempPassword1!",
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!"
            };
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword(dto.CurrentPassword, user.PasswordHash)).Returns(true);
            _hasherMock.Setup(h => h.VerifyPassword(dto.NewPassword, user.PasswordHash)).Returns(false);
            _hasherMock.Setup(h => h.HashPassword(dto.NewPassword)).Returns("new-hash");
            _jwtMock.Setup(j => j.GenerateToken(user)).Returns("novo-jwt");
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.ChangeInitialPasswordAsync(dto));
            Assert.Equal("Credenciais inválidas ou conta inativa.", exception.Message);
        }
        [Fact]
        public async Task ChangeInitialPasswordAsync_QuandoTrocaDeSenhaNaoForNecessaria_DeveLancarInvalidOperationExceptionComMensagem()
        {
            var user = SeedStudent(isPasswordChangeRequired: false);
            var dto = new ChangeInitialPasswordDto
            {
                Email = user.Email,
                CurrentPassword = "TempPassword1!",
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!"
            };
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword(dto.CurrentPassword, user.PasswordHash)).Returns(true);
            _hasherMock.Setup(h => h.VerifyPassword(dto.NewPassword, user.PasswordHash)).Returns(false);
            _hasherMock.Setup(h => h.HashPassword(dto.NewPassword)).Returns("new-hash");
            _jwtMock.Setup(j => j.GenerateToken(user)).Returns("novo-jwt");
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.ChangeInitialPasswordAsync(dto));
            Assert.Equal("Este usuário não possui pendência de redefinição obrigatória de senha.", exception.Message);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_QuandoUsuarioAtivo_DeveGerarTokenEEnviarEmail()
        {
            var user = SeedStudent();
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("reset-hash");

            await _authService.RequestPasswordResetAsync(new ForgotPasswordRequestDto { Email = user.Email });

            Assert.Equal("reset-hash", user.PasswordResetTokenHash);
            Assert.NotNull(user.PasswordResetExpiresAt);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<EmailAddress>(), "Redefinição de Senha", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_QuandoTokenExpirado_DeveLancarKeyNotFoundException()
        {
            var user = SeedStudent();
            user.PasswordResetTokenHash = "reset-hash";
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(-30);
            var dto = new ResetPasswordRequestDto
            {
                Email = user.Email,
                Token = "token-valido",
                NewPassword = "NovaSenha@123",
                ConfirmPassword = "NovaSenha@123"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword(dto.Token, "reset-hash")).Returns(true);
            _hasherMock.Setup(h => h.HashPassword(dto.NewPassword)).Returns("nova-hash");

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _authService.ResetPasswordAsync(dto));
        }
        [Fact]
        public async Task ResetPasswordAsync_QuandoTokenValidoESenhasIguais_DeveRedefinirSenha()
        {
            var user = SeedStudent();
            user.PasswordResetTokenHash = "reset-hash";
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(30);
            var dto = new ResetPasswordRequestDto
            {
                Email = user.Email,
                Token = "token-valido",
                NewPassword = "NovaSenha@123",
                ConfirmPassword = "NovaSenha@123"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
            _hasherMock.Setup(h => h.VerifyPassword(dto.Token, "reset-hash")).Returns(true);
            _hasherMock.Setup(h => h.HashPassword(dto.NewPassword)).Returns("nova-hash");

            await _authService.ResetPasswordAsync(dto);

            Assert.Null(user.PasswordResetTokenHash);
            Assert.Null(user.PasswordResetExpiresAt);
            Assert.Equal("nova-hash", user.PasswordHash);
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task AnonymizePersonalDataAsync_QuandoAdministrador_DeveLancarUnauthorizedAccessException()
        {
            var admin = new Administrator
            {
                Id = Guid.NewGuid(),
                Name = "Admin Teste",
                Email = "admin@teste.com",
                PasswordHash = "hash-senha"
            };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);

            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.AnonymizePersonalDataAsync(admin.Id));

            Assert.Equal("O administrador não pode excluir ou anonimizar dados por esta operação.", exception.Message);
            Assert.Equal("Admin Teste", admin.Name);
            Assert.Equal("admin@teste.com", admin.Email);
        }

        [Fact]
        public async Task AnonymizePersonalDataAsync_QuandoAluno_DeveAnonimizarContaHistoricoEEmail()
        {
            var student = new Student
            {
                Id = Guid.NewGuid(),
                Name = "Aluno Teste",
                Email = "aluno@teste.com",
                PasswordHash = "hash-senha",
                IsActive = true,
                ApprovalStatus = UserApprovalStatus.Aprovado,
                UniqueIdentifier = "RGM123456",
                CourseId = Guid.NewGuid(),
                InstitutionId = Guid.NewGuid(),
                TwoFactorEnabled = true,
                TwoFactorSecretKey = "SECRET123"
            };
            _dbContext.Students.Add(student);
            _dbContext.AuditLogs.Add(new AuditLog
            {
                EntityId = student.Id.ToString(),
                EntityName = "Student",
                OperationType = AuditOperationType.Update,
                UserId = Guid.NewGuid(),
                OldValues = "{\"email\":\"antigo@teste.com\",\"name\":\"Nome Antigo\",\"passwordHash\":\"segredo\"}",
                NewValues = "{\"email\":\"aluno@teste.com\",\"name\":\"Aluno Teste\"}"
            });
            _dbContext.AuditLogs.Add(new AuditLog
            {
                EntityId = Guid.NewGuid().ToString(),
                EntityName = "Event",
                OperationType = AuditOperationType.Update,
                UserId = student.Id,
                OldValues = "{\"name\":\"Semana Academica\"}",
                NewValues = "{\"name\":\"Semana Academica\"}",
                Description = "Enviado para aluno@teste.com"
            });
            await _dbContext.SaveChangesAsync();

            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);
            _userRepositoryMock.Setup(r => r.UpdateAsync(student)).Returns(() => _dbContext.SaveChangesAsync());
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("anon-hash");

            await _authService.AnonymizePersonalDataAsync(student.Id);

            Assert.Equal("Usuário anonimizado", student.Name);
            Assert.StartsWith("anonimizado-", student.Email);
            Assert.EndsWith("@removido.local", student.Email);
            Assert.False(student.IsActive);
            Assert.False(student.TwoFactorEnabled);
            Assert.Null(student.TwoFactorSecretKey);
            Assert.Equal(20, student.UniqueIdentifier.Length);
            Assert.NotEqual("RGM123456", student.UniqueIdentifier);

            var logs = await _dbContext.AuditLogs.ToListAsync();
            var serialized = string.Join('\n', logs.Select(log => $"{log.OldValues} {log.NewValues} {log.Description}"));
            Assert.DoesNotContain("Aluno Teste", serialized);
            Assert.DoesNotContain("Nome Antigo", serialized);
            Assert.DoesNotContain("aluno@teste.com", serialized);
            Assert.DoesNotContain("antigo@teste.com", serialized);
            Assert.DoesNotContain("segredo", serialized);
            Assert.DoesNotContain("RGM123456", serialized);
            Assert.Contains("Semana Academica", serialized);
            Assert.Contains("Usuário anonimizado", serialized);
        }
    }
}

