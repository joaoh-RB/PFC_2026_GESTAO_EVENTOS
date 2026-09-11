using API_Gestao_Eventos.src.Application.DTO.Auth;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using API_Gestao_Eventos.src.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class AuthService(
        IHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IGoogleAuthService googleAuthService,
        UserRepository userRepository)
    {
        private readonly IHasher _passwordHasher = passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IGoogleAuthService _googleAuthService = googleAuthService;
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
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    InstitutionId = user.InstitutionId
                }
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
                InstitutionId = user.InstitutionId
            };
        }
    }
}
