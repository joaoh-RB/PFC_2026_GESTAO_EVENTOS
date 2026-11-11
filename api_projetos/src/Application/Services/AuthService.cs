using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using API_Gestao_Eventos.src.Common.Utils;
using API_Gestao_Eventos.src.Infrastructure.Services.Email;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class AuthService(
        IHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IGoogleAuthService googleAuthService,
        IEmailService emailService,
        EmailTemplateRenderer emailTemplateRenderer,
        IConfiguration configuration,
        AppDbContext appDbContext,
        IUserRepository userRepository) : IAuthService
    {
        private readonly IHasher _passwordHasher = passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IGoogleAuthService _googleAuthService = googleAuthService;
        private readonly IEmailService _emailService = emailService;
        private readonly EmailTemplateRenderer _emailTemplateRenderer = emailTemplateRenderer;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly AppDbContext _appDbContext = appDbContext;
        public async Task<AuthResponseDto> RegisterStudentAsync(RegisterRequestDto request)
        {
            var emailExists = await _userRepository.ExistsByEmailAsync(request.Email);
            if (emailExists)
                throw new InvalidOperationException("E-mail já cadastrado.");
            var uniqueIdentifierAndInstitutionExists = await _userRepository.ExistsByUniqueIdentifierAndInstitutionAsync(request.UniqueIdentifier, request.InstitutionId);
            if (uniqueIdentifierAndInstitutionExists)
                throw new InvalidOperationException("RGM/Matrícula já cadastrada para esta instituição.");
            var student = new Student
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                UniqueIdentifier = request.UniqueIdentifier,
                InstitutionId = request.InstitutionId,
                CourseId = request.CourseId
            };

            await _userRepository.AddAsync(student);

            return new AuthResponseDto
            {
                RequiresTwoFactor = false,
                RequiresPasswordChange = false,
                Message = "Cadastro realizado com sucesso, seu usuário está pendente de aprovação. Entre em contato com sua instituição em caso de dúvidas. Assim que o acesso for aprovado será enviado um e-mail contendo as informações de acesso."
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciais inválidas.");
            if (!user.IsActive)
                throw new UnauthorizedAccessException("Usuário inativo. Entre em contato com a sua instituição.");
            if (user.ApprovalStatus == UserApprovalStatus.Pendente)
                throw new UnauthorizedAccessException("Seu cadastro ainda não foi aprovado. Entre em contato com a sua instituição.");
            if (user.ApprovalStatus == UserApprovalStatus.Reprovado)
                throw new UnauthorizedAccessException("Seu cadastro ainda não foi aprovado. Entre em contato com a sua instituição.");
            if (user.IsPasswordChangeRequired)
                return new AuthResponseDto()
                {
                    RequiresPasswordChange = true,
                    Message = "É necessário alterar a senha antes de prosseguir."
                };
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    return new AuthResponseDto
                    {
                        RequiresTwoFactor = true,
                        Message = "Código de autenticação de dois fatores é obrigatório."
                    };
                }

                var isCodeValid = _googleAuthService.ValidateTwoFactorCode(user.TwoFactorSecretKey!, request.TwoFactorCode);
                if (!isCodeValid)
                {
                    await _appDbContext.LogAuthEventAsync(
                        AuditOperationType.LoginFailure,
                        "Código 2FA inválido",
                        user.Id
                    );
                    throw new UnauthorizedAccessException("Código de dois fatores inválido.");
                }
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

            await _appDbContext.LogAuthEventAsync(
                AuditOperationType.LoginSuccess,
                "Login realizado com sucesso",
                user.Id
            );

            return new AuthResponseDto
            {
                RequiresTwoFactor = false,
                Token = token,
                User = await GetUserInfoAsync(user.Id)
            };
        }

        public async Task<TwoFactorCreateResponseDto> SetupTwoFactorAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NullReferenceException("Usuário não encontrado.");

            var secret = _googleAuthService.GenerateSecretKey();
            user.TwoFactorSecretKey = secret;
            await _userRepository.UpdateAsync(user);

            var uri = _googleAuthService.GenerateQrCodeUri(user.Email, secret);

            return new TwoFactorCreateResponseDto
            {
                SecretKey = secret,
                QrCodeUrl = uri
            };
        }

        public async Task<bool> EnableTwoFactorAsync(Guid userId, string code)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NullReferenceException("Usuário não encontrado.");

            if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
                throw new InvalidOperationException("Configure a chave 2FA antes de ativá-la.");

            var isValid = _googleAuthService.ValidateTwoFactorCode(user.TwoFactorSecretKey, code);
            if (!isValid) return false;

            user.TwoFactorEnabled = true;
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task DisableTwoFactorAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (!user.TwoFactorEnabled)
                throw new InvalidOperationException("A autenticação em duas etapas já está desativada.");

            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            await _appDbContext.LogAuthEventAsync(
                AuditOperationType.Update,
                "Autenticação em duas etapas desativada",
                user.Id
            );
        }
        public async Task<AuthResponseDto> ChangeInitialPasswordAsync(ChangeInitialPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Credenciais inválidas ou conta inativa.");

            var verificationResult = _passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash);
            if (!verificationResult)
            {
                await _appDbContext.LogAuthEventAsync(
                    AuditOperationType.PasswordChanged,
                    "Falha ao tentar alterar senha - senha atual incorreta",
                    user.Id
                );
                throw new UnauthorizedAccessException("A senha atual informada está incorreta.");
            }

            if (!user.IsPasswordChangeRequired)
                throw new InvalidOperationException("Este usuário não possui pendência de redefinição obrigatória de senha.");

            var checkSamePassword = _passwordHasher.VerifyPassword(dto.NewPassword, user.PasswordHash);
            if (checkSamePassword)
                throw new InvalidOperationException("A nova senha não pode ser idêntica à senha temporária.");

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.IsPasswordChangeRequired = false;

            await _userRepository.UpdateAsync(user);

            await _appDbContext.LogAuthEventAsync(
                AuditOperationType.PasswordChanged,
                "Senha inicial alterada com sucesso",
                user.Id
            );

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResponseDto
            {
                RequiresTwoFactor = false,
                RequiresPasswordChange = false,
                Token = token,
                Message = "Senha alterada e conta ativada com sucesso!",
                User = await GetUserInfoAsync(user.Id)
            };
        }
        public async Task RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user != null && user.IsActive)
            {
                var resetToken = GeneratePassword.Generate(12);
                user.PasswordResetTokenHash = _passwordHasher.HashPassword(resetToken);
                user.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
                await _userRepository.UpdateAsync(user);

                await _appDbContext.LogAuthEventAsync(
                    AuditOperationType.PasswordReset,
                    "Solicitação de redefinição de senha enviada por e-mail",
                    user.Id
                );

                var siteAddress = configuration.GetSection("FrontendInfo")["BaseUrl"];
                var accessLink = siteAddress + "/reset-password?token=" + resetToken + "&email=" + Uri.EscapeDataString(user.Email);
                var resetPasswordEmail = await _emailTemplateRenderer.RenderAsync("notificacao-generica.html", new Dictionary<string, string>
                {
                    ["titulo"] = "Redefinição de senha",
                    ["nome"] = user.Name,
                    ["mensagem"] = $"Você solicitou a redefinição de senha. Use o código temporário <strong>{resetToken}</strong> e siga para a tela de redefinição.",
                    ["link_acao"] = accessLink,
                    ["texto_botao"] = "Alterar senha"
                });
                await _emailService.SendEmailAsync(new SendGrid.Helpers.Mail.EmailAddress(user.Email, user.Name), "Redefinição de Senha", resetPasswordEmail);
            }
            else
            {
                Thread.Sleep(2000);
            }
        }
        public async Task ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new KeyNotFoundException("Email inválido ou token expirado. Peça o reenvio da redefinição de senha.");
            var validToken = DateTime.UtcNow <= user.PasswordResetExpiresAt && !string.IsNullOrEmpty(user.PasswordResetTokenHash) && _passwordHasher.VerifyPassword(dto.Token, user.PasswordResetTokenHash);
            if (!validToken)
                throw new KeyNotFoundException("Email inválido ou token expirado. Peça o reenvio da redefinição de senha");
            if (dto.NewPassword != dto.ConfirmPassword)
                throw new InvalidOperationException("A senha não coincide com a confirmação.");
            user.PasswordResetExpiresAt = null;
            user.PasswordResetTokenHash = null;
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            await _appDbContext.LogAuthEventAsync(
                AuditOperationType.PasswordReset,
                "Senha redefinida com sucesso",
                user.Id
            );
        }

        public async Task AnonymizePersonalDataAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.Role == UserRole.Administrador)
                throw new UnauthorizedAccessException("O administrador não pode excluir ou anonimizar dados por esta operação.");

            var originalName = user.Name;
            var originalEmail = user.Email;
            var token = Guid.NewGuid().ToString("N");
            var anonymizedName = "Usuário anonimizado";
            var anonymizedEmail = $"anonimizado-{token}@removido.local";
            var anonymizedIdentifier = token[..20];

            user.Name = anonymizedName;
            user.Email = anonymizedEmail;
            user.PasswordHash = _passwordHasher.HashPassword(token);
            user.IsActive = false;
            user.IsPasswordChangeRequired = false;
            user.PasswordResetTokenHash = null;
            user.PasswordResetExpiresAt = null;
            user.TwoFactorEnabled = false;
            user.TwoFactorSecretKey = null;
            user.UpdatedAt = DateTime.UtcNow;

            if (user is Student student)
                student.UniqueIdentifier = anonymizedIdentifier;

            await _userRepository.UpdateAsync(user);
            await AnonymizeLinkedHistoryAsync(user.Id, originalName, originalEmail, anonymizedName, anonymizedEmail, anonymizedIdentifier);
            await _appDbContext.LogAuthEventAsync(
                AuditOperationType.Delete,
                "Dados pessoais anonimizados",
                user.Id
            );
        }

        private async Task AnonymizeLinkedHistoryAsync(
            Guid userId,
            string originalName,
            string originalEmail,
            string anonymizedName,
            string anonymizedEmail,
            string anonymizedIdentifier)
        {
            var userIdText = userId.ToString();
            var logs = await _appDbContext.AuditLogs
                .Where(log => log.UserId == userId || log.EntityId == userIdText)
                .ToListAsync();

            foreach (var log in logs)
            {
                var isUserRecord = string.Equals(log.EntityId, userIdText, StringComparison.OrdinalIgnoreCase);
                if (isUserRecord)
                {
                    log.OldValues = RedactUserIdentitySnapshot(log.OldValues, anonymizedName, anonymizedEmail, anonymizedIdentifier);
                    log.NewValues = RedactUserIdentitySnapshot(log.NewValues, anonymizedName, anonymizedEmail, anonymizedIdentifier);
                }
                else
                {
                    log.OldValues = ReplaceLiteral(log.OldValues, originalEmail, anonymizedEmail);
                    log.NewValues = ReplaceLiteral(log.NewValues, originalEmail, anonymizedEmail);
                }

                log.Description = ReplaceLiteral(log.Description, originalEmail, anonymizedEmail);
                log.Description = ReplaceLiteral(log.Description, originalName, anonymizedName);
            }

            if (logs.Count > 0)
                await _appDbContext.SaveChangesAsync();
        }

        private static string? RedactUserIdentitySnapshot(string? payload, string anonymizedName, string anonymizedEmail, string anonymizedIdentifier)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return payload;

            try
            {
                if (JsonNode.Parse(payload) is not JsonObject snapshot)
                    return payload;

                foreach (var property in snapshot.ToList())
                {
                    if (property.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
                        snapshot[property.Key] = anonymizedName;
                    else if (property.Key.Equals("email", StringComparison.OrdinalIgnoreCase))
                        snapshot[property.Key] = anonymizedEmail;
                    else if (property.Key.Equals("uniqueIdentifier", StringComparison.OrdinalIgnoreCase))
                        snapshot[property.Key] = anonymizedIdentifier;
                    else if (IsSecretAuditProperty(property.Key))
                        snapshot[property.Key] = null;
                }

                return snapshot.ToJsonString(new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (JsonException)
            {
                return payload;
            }
        }

        private static bool IsSecretAuditProperty(string propertyName)
        {
            return propertyName.Equals("passwordHash", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("twoFactorSecretKey", StringComparison.OrdinalIgnoreCase)
                || propertyName.Equals("passwordResetTokenHash", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ReplaceLiteral(string? value, string original, string replacement)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(original) || original.Length < 3)
                return value;

            return value.Replace(original, replacement, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<UserResponseDto> GetUserInfoAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NullReferenceException("Usuário não encontrado.");
            return new UserResponseDto()
            {
                Email = user.Email,
                Role = user.Role.ToString(),
                Id = user.Id,
                Name = user.Name,
                TwoFactorEnabled = user.TwoFactorEnabled,
                InstitutionId = user.InstitutionId,
                InstitutionName = user.Institution?.Name ?? ""
            };
        }
    }
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterStudentAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
        Task<TwoFactorCreateResponseDto> SetupTwoFactorAsync(Guid userId);
        Task<bool> EnableTwoFactorAsync(Guid userId, string code);
        Task DisableTwoFactorAsync(Guid userId);
        Task<AuthResponseDto> ChangeInitialPasswordAsync(ChangeInitialPasswordDto dto);
        Task RequestPasswordResetAsync(ForgotPasswordRequestDto dto);
        Task ResetPasswordAsync(ResetPasswordRequestDto dto);
        Task AnonymizePersonalDataAsync(Guid userId);
        Task<UserResponseDto> GetUserInfoAsync(Guid userId);
    }
}
