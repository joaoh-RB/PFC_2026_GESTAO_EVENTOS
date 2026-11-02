using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;
using Xunit;

namespace API_Gestao_Eventos.Tests.Services
{
    public class InstitutionServiceTests
    {
        private const string ValidCnpj = "11.222.333/0001-81";
        private readonly Mock<IInstitutionRepository> _institutionRepositoryMock;
        private readonly InstitutionService _institutionService;

        public InstitutionServiceTests()
        {
            _institutionRepositoryMock = new Mock<IInstitutionRepository>();
            _institutionService = new InstitutionService(_institutionRepositoryMock.Object);
        }

        private static Institution SeedInstitution(Guid? id = null, string name = "Instituto Federal", string? cnpj = ValidCnpj) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : Cnpj.Create(cnpj),
            Address = "Rua Principal, 100",
            Phone = "11999999999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        private static CreateInstitutionDto SeedCreateDto(string name = "Instituto Federal", string? cnpj = ValidCnpj) => new()
        {
            Name = name,
            Cnpj = cnpj,
            Address = "Rua Principal, 100",
            Phone = "11999999999"
        };

        private static UpdateInstitutionDto SeedUpdateDto(string name = "Instituto Atualizado", string? cnpj = ValidCnpj) => new()
        {
            Name = name,
            Cnpj = cnpj,
            Address = "Rua Nova, 200",
            Phone = "11988888888"
        };

        [Fact]
        public async Task GetInstituctionsForSelectAsync_QuandoChamado_DeveRetornarInstituicoesAtivasMapeadas()
        {
            var institution = SeedInstitution();
            _institutionRepositoryMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new[] { institution });

            var result = (await _institutionService.GetInstituctionsForSelectAsync()).ToList();

            Assert.Single(result);
            Assert.Equal(institution.Id.ToString(), result[0].Value);
            Assert.Equal(institution.Name, result[0].Label);
        }

        [Fact]
        public async Task CreateInstitutionAsync_QuandoNomeJaExistir_DeveLancarInvalidOperationException()
        {
            var dto = SeedCreateDto();
            _institutionRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _institutionService.CreateInstitutionAsync(dto));

            Assert.Equal("Instituição já cadastrada.", exception.Message);
            _institutionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Institution>()), Times.Never);
        }

        [Fact]
        public async Task CreateInstitutionAsync_QuandoCnpjJaExistir_DeveLancarInvalidOperationException()
        {
            var dto = SeedCreateDto();
            _institutionRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _institutionRepositoryMock.Setup(r => r.ExistsByCnpjAsync(It.IsAny<Cnpj>())).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _institutionService.CreateInstitutionAsync(dto));

            Assert.Equal("CNPJ já cadastrado para outra instituição.", exception.Message);
            _institutionRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Institution>()), Times.Never);
        }

        [Theory]
        [InlineData(ValidCnpj)]
        [InlineData(null)]
        public async Task CreateInstitutionAsync_QuandoDadosValidos_DeveCadastrarERetornarId(string? cnpj)
        {
            var dto = SeedCreateDto(cnpj: cnpj);
            _institutionRepositoryMock.Setup(r => r.ExistsByNameAsync(dto.Name)).ReturnsAsync(false);
            _institutionRepositoryMock.Setup(r => r.ExistsByCnpjAsync(It.IsAny<Cnpj>())).ReturnsAsync(false);

            var result = await _institutionService.CreateInstitutionAsync(dto);

            Assert.NotEqual(Guid.Empty, result);
            _institutionRepositoryMock.Verify(r => r.AddAsync(It.Is<Institution>(i => i.Name == dto.Name)), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_QuandoInstituicaoNaoExistir_DeveRetornarNulo()
        {
            var id = Guid.NewGuid();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Institution?)null);

            var result = await _institutionService.GetByIdAsync(id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_QuandoInstituicaoExistir_DeveRetornarDetalhesFormatados()
        {
            var institution = SeedInstitution();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);

            var result = await _institutionService.GetByIdAsync(institution.Id);

            Assert.NotNull(result);
            Assert.Equal(institution.Id, result.Id);
            Assert.Equal(institution.Name, result.Name);
            Assert.Equal(ValidCnpj, result.Cnpj);
        }

        [Fact]
        public async Task UpdateInstitutionAsync_QuandoInstituicaoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Institution?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _institutionService.UpdateInstitutionAsync(id, SeedUpdateDto()));

            Assert.Equal("Instituição não encontrada.", exception.Message);
        }

        [Fact]
        public async Task UpdateInstitutionAsync_QuandoNomeJaExistirEmOutraInstituicao_DeveLancarInvalidOperationException()
        {
            var institution = SeedInstitution();
            var dto = SeedUpdateDto();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);
            _institutionRepositoryMock.Setup(r => r.ExistsByNameExceptAsync(dto.Name, institution.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _institutionService.UpdateInstitutionAsync(institution.Id, dto));

            Assert.Equal("Já existe outra instituição com este nome.", exception.Message);
        }

        [Fact]
        public async Task UpdateInstitutionAsync_QuandoCnpjJaExistirEmOutraInstituicao_DeveLancarInvalidOperationException()
        {
            var institution = SeedInstitution();
            var dto = SeedUpdateDto();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);
            _institutionRepositoryMock.Setup(r => r.ExistsByNameExceptAsync(dto.Name, institution.Id)).ReturnsAsync(false);
            _institutionRepositoryMock.Setup(r => r.ExistsByCnpjExceptAsync(It.IsAny<Cnpj>(), institution.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _institutionService.UpdateInstitutionAsync(institution.Id, dto));

            Assert.Equal("CNPJ já cadastrado para outra instituição.", exception.Message);
        }

        [Theory]
        [InlineData(ValidCnpj)]
        [InlineData(null)]
        public async Task UpdateInstitutionAsync_QuandoDadosValidos_DeveAtualizarInstituicao(string? cnpj)
        {
            var institution = SeedInstitution();
            var dto = SeedUpdateDto(cnpj: cnpj);
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);
            _institutionRepositoryMock.Setup(r => r.ExistsByNameExceptAsync(dto.Name, institution.Id)).ReturnsAsync(false);
            _institutionRepositoryMock.Setup(r => r.ExistsByCnpjExceptAsync(It.IsAny<Cnpj>(), institution.Id)).ReturnsAsync(false);

            await _institutionService.UpdateInstitutionAsync(institution.Id, dto);

            Assert.Equal(dto.Name, institution.Name);
            Assert.Equal(dto.Address, institution.Address);
            Assert.Equal(dto.Phone, institution.Phone);
            _institutionRepositoryMock.Verify(r => r.UpdateAsync(institution), Times.Once);
        }

        [Fact]
        public async Task GetAllForManagementAsync_QuandoChamado_DeveRetornarListaMapeada()
        {
            var institution = SeedInstitution();
            _institutionRepositoryMock.Setup(r => r.GetAllForManagementAsync()).ReturnsAsync(new[] { institution });

            var result = (await _institutionService.GetAllForManagementAsync()).ToList();

            Assert.Single(result);
            Assert.Equal(institution.Id, result[0].Id);
            Assert.Equal(institution.Name, result[0].Name);
            Assert.Equal(institution.IsActive, result[0].IsActive);
        }

        [Fact]
        public async Task SetInstitutionActiveAsync_QuandoInstituicaoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Institution?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _institutionService.SetInstitutionActiveAsync(id, false));

            Assert.Equal("Instituição não encontrada.", exception.Message);
        }

        [Fact]
        public async Task SetInstitutionActiveAsync_QuandoInativarComDependencias_DeveLancarInvalidOperationException()
        {
            var institution = SeedInstitution();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);
            _institutionRepositoryMock.Setup(r => r.HasDependenciesAsync(institution.Id)).ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _institutionService.SetInstitutionActiveAsync(institution.Id, false));

            Assert.Equal("Não é possível inativar: existem usuários, cursos ou eventos vinculados a esta instituição.", exception.Message);
            _institutionRepositoryMock.Verify(r => r.SetActiveAsync(It.IsAny<Institution>(), It.IsAny<bool>()), Times.Never);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetInstitutionActiveAsync_QuandoSemImpedimentos_DeveAlterarStatusAtivo(bool isActive)
        {
            var institution = SeedInstitution();
            _institutionRepositoryMock.Setup(r => r.GetByIdAsync(institution.Id)).ReturnsAsync(institution);
            _institutionRepositoryMock.Setup(r => r.HasDependenciesAsync(institution.Id)).ReturnsAsync(false);

            await _institutionService.SetInstitutionActiveAsync(institution.Id, isActive);

            _institutionRepositoryMock.Verify(r => r.SetActiveAsync(institution, isActive), Times.Once);
        }
    }
}

