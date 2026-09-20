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
using System.Text;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class AuthService(
        IHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IGoogleAuthService googleAuthService,
        EmailService emailService,
        IConfiguration configuration,
        UserRepository userRepository)
    {
        private readonly IHasher _passwordHasher = passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IGoogleAuthService _googleAuthService = googleAuthService;
        private readonly EmailService _emailService = emailService;
        private readonly UserRepository _userRepository = userRepository;

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
                    throw new UnauthorizedAccessException("Código de dois fatores inválido.");
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

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
        public async Task<AuthResponseDto> ChangeInitialPasswordAsync(ChangeInitialPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Credenciais inválidas ou conta inativa.");

            var verificationResult = _passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash);
            if (!verificationResult)
                throw new UnauthorizedAccessException("A senha atual informada está incorreta.");

            if (!user.IsPasswordChangeRequired)
                throw new InvalidOperationException("Este usuário não possui pendência de redefinição obrigatória de senha.");

            var checkSamePassword = _passwordHasher.VerifyPassword(dto.NewPassword, user.PasswordHash);
            if (checkSamePassword)
                throw new InvalidOperationException("A nova senha não pode ser idêntica à senha temporária.");

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            user.IsPasswordChangeRequired = false;

            await _userRepository.UpdateAsync(user);

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
                var sbEmailBody = new StringBuilder();
                sbEmailBody.Append("Olá, " + user.Name + "<br/>");
                sbEmailBody.Append("Você solicitou a redefinição de senha. <br/>");
                sbEmailBody.Append("Para alterar a senha, clique no link abaixo e utilize o código temporário: <br/>");
                var siteAddress = configuration.GetSection("FrontendInfo")["BaseUrl"];
                var accessLink = siteAddress + "/reset-password?token=" + resetToken + "&email=" + user.Email;
                sbEmailBody.Append("<a href='" + siteAddress + "'>Alterar Senha</a><br/>");
                sbEmailBody.Append("Caso o link não tenha funcionado, copie e cole no seu navegador: " + accessLink);
                await _emailService.SendEmailAsync(new SendGrid.Helpers.Mail.EmailAddress(user.Email, user.Name), "Redefinição de Senha", sbEmailBody.ToString());
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
}
