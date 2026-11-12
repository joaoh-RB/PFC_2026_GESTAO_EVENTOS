using API_Gestao_Eventos.src.Application.DTO.User;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Email;
using API_Gestao_Eventos.src.Infrastructure.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using SendGrid.Helpers.Mail;

namespace API_Gestao_Eventos.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IInstitutionRepository> _institutionRepositoryMock;
        private readonly Mock<ICourseRepository> _courseRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IHasher> _hasherMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _institutionRepositoryMock = new Mock<IInstitutionRepository>();
            _courseRepositoryMock = new Mock<ICourseRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _hasherMock = new Mock<IHasher>();

            var tempRoot = Path.Combine(Path.GetTempPath(), "symplosio_tests_env");
            var apiDir = Path.Combine(tempRoot, "api_projetos");
            var templatesDir = Path.Combine(apiDir, "public", "Templates");
            Directory.CreateDirectory(templatesDir);
            var templateFile = Path.Combine(templatesDir, "notificacao-generica.html");
            if (!File.Exists(templateFile))
                File.WriteAllText(templateFile, "<html>{{titulo}} {{nome}} {{mensagem}} {{link_acao}} {{texto_botao}}</html>");

            var hostEnvMock = new Mock<IHostEnvironment>();
            hostEnvMock.Setup(h => h.ContentRootPath).Returns(apiDir);
            var renderer = new EmailTemplateRenderer(hostEnvMock.Object);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["FrontendInfo:BaseUrl"] = "http://localhost:3000" })
                .Build();

            _userService = new UserService(
                _userRepositoryMock.Object,
                _institutionRepositoryMock.Object,
                _courseRepositoryMock.Object,
                _emailServiceMock.Object,
                renderer,
                config,
                _hasherMock.Object);
        }

        private static Student SeedStudent(Guid? id = null, Guid? institutionId = null, Guid? courseId = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Aluno Teste",
            Email = "aluno@teste.com",
            UniqueIdentifier = "RGM123",
            InstitutionId = institutionId ?? Guid.NewGuid(),
            CourseId = courseId ?? Guid.NewGuid(),
            ApprovalStatus = UserApprovalStatus.Pendente,
            IsActive = true
        };

        private static Teacher SeedTeacher(Guid institutionId, Guid courseId) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Professor Teste",
            Email = "professor@teste.com",
            InstitutionId = institutionId,
            Institution = new Institution { Id = institutionId, Name = "Instituição Teste" },
            Courses = new List<Course> { new() { Id = courseId, Name = "ADS" } },
            IsActive = true
        };

        private void SeedValidInstitutionAndCourse(Guid institutionId, Guid courseId)
        {
            _institutionRepositoryMock.Setup(r => r.GetAllActiveAsync())
                .ReturnsAsync(new[] { new Institution { Id = institutionId, Name = "Instituição Teste", IsActive = true } });
            _courseRepositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new[] { new Course { Id = courseId, Name = "ADS", InstitutionId = institutionId, IsActive = true } });
        }

        [Fact]
        public async Task GetAllAsync_EGetByIdAsync_DevemRetornarAlunosMapeados()
        {
            var student = SeedStudent();
            _userRepositoryMock.Setup(r => r.GetAllStudentsAsync()).ReturnsAsync(new[] { student });
            _userRepositoryMock.Setup(r => r.GetStudentByIdAsync(student.Id)).ReturnsAsync(student);

            var all = (await _userService.GetAllAsync()).ToList();
            var byId = await _userService.GetByIdAsync(student.Id);

            Assert.Single(all);
            Assert.Equal(student.Id, byId.Id);
            Assert.Equal(student.Name, byId.Name);
        }

        [Fact]
        public async Task CreateInstitutionMember_QuandoProfessorValido_DeveCriarProfessorEnviarEmailERetornarDto()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var dto = new CreateInstitutionMemberRequestDto
            {
                Name = " Professor Novo ",
                Email = " prof@teste.com ",
                InstitutionId = institutionId,
                UserRole = UserRole.Professor,
                Courses = new List<Guid> { courseId }
            };

            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(institutionId))
                .ReturnsAsync(new[] { new Course { Id = courseId, Name = "ADS", InstitutionId = institutionId } });
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");
            _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => SeedTeacher(institutionId, courseId));

            var result = await _userService.CreateInstitutionMember(dto);

            Assert.Equal(nameof(UserRole.Professor), result.UserRole);
            Assert.Contains(courseId, result.CoursesIds!);
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<Teacher>(t => t.Name == "Professor Novo" && t.IsPasswordChangeRequired)), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<EmailAddress>(), "Bem-vindo ao Sistema", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateInstitutionMember_QuandoCursoInvalidoOuEmailDuplicado_DeveLancarInvalidOperationException()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var dto = new CreateInstitutionMemberRequestDto
            {
                Name = "Professor",
                Email = "prof@teste.com",
                InstitutionId = institutionId,
                UserRole = UserRole.Professor,
                Courses = new List<Guid> { courseId }
            };

            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(institutionId)).ReturnsAsync(Array.Empty<Course>());
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateInstitutionMember(dto));

            dto.UserRole = UserRole.Secretaria;
            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateInstitutionMember(dto));
        }

        [Fact]
        public async Task GetInstitutionMembersPagedAsync_QuandoChamado_DeveRetornarPaginado()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var teacher = SeedTeacher(institutionId, courseId);
            var filter = new InstitutionMemberFilterDto { PageNumber = 1, PageSize = 10 };

            _userRepositoryMock.Setup(r => r.GetFilteredInstitutionMembersAsync(filter))
                .ReturnsAsync((new User[] { teacher }, 1));

            var result = await _userService.GetInstitutionMembersPagedAsync(filter);

            Assert.Equal(1, result.TotalItems);
            Assert.Single(result.Items);
            Assert.Equal(teacher.Id, result.Items.First().Id);
        }

        [Fact]
        public async Task UpdateInstitutionMemberAsync_QuandoProfessorValido_DeveAtualizarDadosECursos()
        {
            var institutionId = Guid.NewGuid();
            var oldCourseId = Guid.NewGuid();
            var newCourseId = Guid.NewGuid();
            var teacher = SeedTeacher(institutionId, oldCourseId);
            var request = new UpdateInstitutionMemberRequestDto
            {
                Name = " Professor Atualizado ",
                Email = " novo@teste.com ",
                Courses = new List<Guid> { newCourseId }
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _userRepositoryMock.Setup(r => r.ExistsByEmailExceptAsync(request.Email, teacher.Id)).ReturnsAsync(false);
            _courseRepositoryMock.Setup(r => r.GetByInstitutionAsync(institutionId))
                .ReturnsAsync(new[] { new Course { Id = newCourseId, Name = "Engenharia" } });

            await _userService.UpdateInstitutionMemberAsync(teacher.Id, request);

            Assert.Equal("Professor Atualizado", teacher.Name);
            Assert.Equal("novo@teste.com", teacher.Email);
            Assert.Single(teacher.Courses);
            Assert.Equal(newCourseId, teacher.Courses.First().Id);
            _userRepositoryMock.Verify(r => r.UpdateAsync(teacher), Times.Once);
        }

        [Fact]
        public async Task CreateStudentAsync_QuandoDadosValidos_DeveCriarAlunoAprovadoEEnviarEmail()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            SeedValidInstitutionAndCourse(institutionId, courseId);

            var request = new CreateStudentRequestDto
            {
                Name = " Maria Silva ",
                Email = " maria@teste.com ",
                UniqueIdentifier = " RGM999 ",
                InstitutionId = institutionId,
                CourseId = courseId
            };

            _userRepositoryMock.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUniqueIdentifierAndInstitutionAsync(request.UniqueIdentifier, institutionId)).ReturnsAsync(false);
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hash");

            var result = await _userService.CreateStudentAsync(request);

            Assert.Equal("Maria Silva", result.Name);
            Assert.Equal(UserApprovalStatus.Aprovado, result.ApprovalStatus);
            _userRepositoryMock.Verify(r => r.AddAsync(It.Is<Student>(s => s.IsPasswordChangeRequired)), Times.Once);
            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<EmailAddress>(), "Bem-vindo ao Sistema", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_QuandoAlunoValido_DeveAtualizarERetornarDto()
        {
            var institutionId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var student = SeedStudent(institutionId: institutionId, courseId: courseId);
            SeedValidInstitutionAndCourse(institutionId, courseId);

            var request = new UpdateStudentRequestDto
            {
                Name = " Aluno Atualizado ",
                Email = " atualizado@teste.com ",
                UniqueIdentifier = " RGM777 ",
                InstitutionId = institutionId,
                CourseId = courseId
            };

            _userRepositoryMock.Setup(r => r.GetStudentByIdAsync(student.Id)).ReturnsAsync(student);
            _userRepositoryMock.Setup(r => r.ExistsByEmailExceptAsync(request.Email, student.Id)).ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.ExistsByUniqueIdentifierAndInstitutionExceptAsync(request.UniqueIdentifier, institutionId, student.Id)).ReturnsAsync(false);

            var result = await _userService.UpdateAsync(student.Id, request);

            Assert.Equal("Aluno Atualizado", result.Name);
            Assert.Equal("atualizado@teste.com", result.Email);
            Assert.Equal("RGM777", result.UniqueIdentifier);
            _userRepositoryMock.Verify(r => r.UpdateAsync(student), Times.Once);
        }

        [Fact]
        public async Task SetApprovalAsync_ESetActiveAsync_DevemValidarEAtualizarUsuario()
        {
            var student = SeedStudent();
            var approverId = Guid.NewGuid();
            _userRepositoryMock.Setup(r => r.GetStudentByIdAsync(student.Id)).ReturnsAsync(student);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.SetApprovalAsync(student.Id, UserApprovalStatus.Pendente, approverId));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.SetApprovalAsync(student.Id, UserApprovalStatus.Aprovado, student.Id));

            await _userService.SetApprovalAsync(student.Id, UserApprovalStatus.Aprovado, approverId);
            await _userService.SetActiveAsync(student.Id, false);

            Assert.Equal(UserApprovalStatus.Aprovado, student.ApprovalStatus);
            Assert.Equal(approverId, student.ApprovedByUserId);
            Assert.False(student.IsActive);
            _userRepositoryMock.Verify(r => r.UpdateAsync(student), Times.Exactly(2));
        }

        [Fact]
        public async Task DisableTwoFactorAsync_QuandoSecretariaDaMesmaInstituicao_DeveDesativar()
        {
            var institutionId = Guid.NewGuid();
            var secretary = new AcademicDepartment
            {
                Id = Guid.NewGuid(),
                Name = "Secretaria",
                Email = "secretaria@teste.com",
                InstitutionId = institutionId
            };
            var student = SeedStudent(institutionId: institutionId);
            student.TwoFactorEnabled = true;
            student.TwoFactorSecretKey = "SECRET";
            _userRepositoryMock.Setup(r => r.GetByIdAsync(secretary.Id)).ReturnsAsync(secretary);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            await _userService.DisableTwoFactorAsync(student.Id, secretary.Id);

            Assert.False(student.TwoFactorEnabled);
            Assert.Null(student.TwoFactorSecretKey);
            _userRepositoryMock.Verify(r => r.UpdateAsync(student), Times.Once);
        }

        [Fact]
        public async Task DisableTwoFactorAsync_QuandoSecretariaDeOutraInstituicao_DeveNegar()
        {
            var secretary = new AcademicDepartment
            {
                Id = Guid.NewGuid(),
                Name = "Secretaria",
                Email = "secretaria@teste.com",
                InstitutionId = Guid.NewGuid()
            };
            var student = SeedStudent();
            student.TwoFactorEnabled = true;
            _userRepositoryMock.Setup(r => r.GetByIdAsync(secretary.Id)).ReturnsAsync(secretary);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.DisableTwoFactorAsync(student.Id, secretary.Id));
        }

        [Fact]
        public async Task DisableTwoFactorAsync_QuandoProfessor_DeveNegar()
        {
            var institutionId = Guid.NewGuid();
            var teacher = SeedTeacher(institutionId, Guid.NewGuid());
            var student = SeedStudent(institutionId: institutionId);
            student.TwoFactorEnabled = true;
            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(student.Id)).ReturnsAsync(student);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _userService.DisableTwoFactorAsync(student.Id, teacher.Id));
        }
    }
}

