using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Moq;

namespace API_Gestao_Eventos.Tests.Services
{
    public class LegalDocumentServiceTests
    {
        private readonly Mock<ILegalDocumentRepository> _legalDocumentRepositoryMock;
        private readonly Mock<ITermAcceptanceRepository> _termAcceptanceRepositoryMock;
        private readonly LegalDocumentService _legalDocumentService;

        public LegalDocumentServiceTests()
        {
            _legalDocumentRepositoryMock = new Mock<ILegalDocumentRepository>();
            _termAcceptanceRepositoryMock = new Mock<ITermAcceptanceRepository>();
            _legalDocumentService = new LegalDocumentService(_legalDocumentRepositoryMock.Object, _termAcceptanceRepositoryMock.Object);
        }

        private static LegalDocument SeedDocument(LegalDocumentType type = LegalDocumentType.TermosDeUso, bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Version = "1.0",
            Content = "Conteúdo do termo",
            EffectiveDate = DateTime.UtcNow,
            IsActive = isActive
        };

        [Fact]
        public async Task GetPendingDocumentsAsync_QuandoDocumentoAtivoNaoFoiAceito_DeveRetornarNaListaDePendentes()
        {
            var userId = Guid.NewGuid();
            var docPrivacidade = SeedDocument(LegalDocumentType.PoliticaPrivacidade);
            var docTermos = SeedDocument(LegalDocumentType.TermosDeUso);

            _legalDocumentRepositoryMock.Setup(r => r.GetActiveByTypeAsync(LegalDocumentType.PoliticaPrivacidade)).ReturnsAsync(docPrivacidade);
            _legalDocumentRepositoryMock.Setup(r => r.GetActiveByTypeAsync(LegalDocumentType.TermosDeUso)).ReturnsAsync(docTermos);
            _termAcceptanceRepositoryMock.Setup(r => r.HasAcceptedAsync(userId, docPrivacidade.Id)).ReturnsAsync(true);
            _termAcceptanceRepositoryMock.Setup(r => r.HasAcceptedAsync(userId, docTermos.Id)).ReturnsAsync(false);

            var result = (await _legalDocumentService.GetPendingDocumentsAsync(userId)).ToList();

            Assert.Single(result);
            Assert.Equal(docTermos.Id, result[0].Id);
            Assert.Equal(docTermos.Type.ToString(), result[0].Type);
        }

        [Fact]
        public async Task GetPendingDocumentsAsync_QuandoNaoHouverDocumentosAtivos_DeveRetornarListaVazia()
        {
            var userId = Guid.NewGuid();
            _legalDocumentRepositoryMock.Setup(r => r.GetActiveByTypeAsync(It.IsAny<LegalDocumentType>())).ReturnsAsync((LegalDocument?)null);

            var result = await _legalDocumentService.GetPendingDocumentsAsync(userId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCurrentDocumentsAsync_QuandoChamado_DeveRetornarDocumentosAtivosMapeados()
        {
            var doc = SeedDocument();
            _legalDocumentRepositoryMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync(new[] { doc });

            var result = (await _legalDocumentService.GetCurrentDocumentsAsync()).ToList();

            Assert.Single(result);
            Assert.Equal(doc.Id, result[0].Id);
            Assert.Equal(doc.Version, result[0].Version);
            Assert.Equal(doc.Content, result[0].Content);
        }

        [Fact]
        public async Task AcceptAsync_QuandoDocumentoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var docId = Guid.NewGuid();
            _legalDocumentRepositoryMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync((LegalDocument?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _legalDocumentService.AcceptAsync(Guid.NewGuid(), new List<Guid> { docId }, "127.0.0.1"));

            Assert.Equal("Documento não encontrado.", exception.Message);
        }

        [Fact]
        public async Task AcceptAsync_QuandoDocumentoEstiverInativo_DeveLancarInvalidOperationException()
        {
            var doc = SeedDocument(isActive: false);
            _legalDocumentRepositoryMock.Setup(r => r.GetByIdAsync(doc.Id)).ReturnsAsync(doc);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _legalDocumentService.AcceptAsync(Guid.NewGuid(), new List<Guid> { doc.Id }, "127.0.0.1"));

            Assert.Equal("Esta versão do documento não está mais vigente.", exception.Message);
        }

        [Fact]
        public async Task AcceptAsync_QuandoDocumentoJaAceitoEOutroNovo_DeveRegistrarApenasONovo()
        {
            var userId = Guid.NewGuid();
            var alreadyAcceptedDoc = SeedDocument(LegalDocumentType.PoliticaPrivacidade);
            var newDoc = SeedDocument(LegalDocumentType.TermosDeUso);

            _legalDocumentRepositoryMock.Setup(r => r.GetByIdAsync(alreadyAcceptedDoc.Id)).ReturnsAsync(alreadyAcceptedDoc);
            _legalDocumentRepositoryMock.Setup(r => r.GetByIdAsync(newDoc.Id)).ReturnsAsync(newDoc);
            _termAcceptanceRepositoryMock.Setup(r => r.HasAcceptedAsync(userId, alreadyAcceptedDoc.Id)).ReturnsAsync(true);
            _termAcceptanceRepositoryMock.Setup(r => r.HasAcceptedAsync(userId, newDoc.Id)).ReturnsAsync(false);

            await _legalDocumentService.AcceptAsync(userId, new List<Guid> { alreadyAcceptedDoc.Id, newDoc.Id }, "192.168.0.1");

            _termAcceptanceRepositoryMock.Verify(r => r.AddAsync(It.Is<TermAcceptance>(t =>
                t.UserId == userId &&
                t.LegalDocumentId == newDoc.Id &&
                t.Version == newDoc.Version &&
                t.IpAddress == "192.168.0.1")), Times.Once);
        }
    }
}

