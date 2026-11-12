using API_Gestao_Eventos.src.Application.Services;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text;

namespace API_Gestao_Eventos.Tests.Services
{
    public class EventDocumentsServiceTests
    {
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly Mock<IStorageService> _storageServiceMock;
        private readonly Mock<IDocumentRepository> _documentRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly EventDocumentsService _eventDocumentsService;

        public EventDocumentsServiceTests()
        {
            _groupRepositoryMock = new Mock<IGroupRepository>();
            _storageServiceMock = new Mock<IStorageService>();
            _documentRepositoryMock = new Mock<IDocumentRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();
            _eventDocumentsService = new EventDocumentsService(
                _groupRepositoryMock.Object,
                _storageServiceMock.Object,
                _documentRepositoryMock.Object,
                _userRepositoryMock.Object,
                _eventRepositoryMock.Object);
        }

        private static Event SeedEvent(bool allowDocuments = true, DateTime? startDate = null, DateTime? endDate = null, Guid? courseId = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Simpósio Acadêmico",
            AllowDocuments = allowDocuments,
            StartDate = startDate ?? DateTime.UtcNow.AddDays(2),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(3),
            InstitutionId = Guid.NewGuid(),
            AllowedCourses = courseId.HasValue ? new List<Course> { new() { Id = courseId.Value, Name = "ADS" } } : new List<Course>()
        };

        private static Group SeedGroup(Event evt, Guid leaderId)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                EventId = evt.Id,
                Event = evt,
                LeaderId = leaderId,
                Leader = new Student { Id = leaderId, Name = "Líder Grupo" },
                Number = 1,
                IsActive = true
            };
            group.Members.Add(new GroupMember { GroupId = group.Id, StudentId = leaderId, EventId = evt.Id, Status = GroupMemberStatus.Accepted });
            return group;
        }

        private static Document SeedDocument(Group group, Guid senderId, DocumentStatus status = DocumentStatus.Pending, string fileName = "artigo.pdf") => new()
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            Group = group,
            SenderId = senderId,
            Sender = new Student { Id = senderId, Name = "Aluno Autor" },
            OriginalFileName = fileName,
            StorageKey = $"groups/{group.Id}/{fileName}",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            Status = status,
            SubmittedAt = DateTime.UtcNow
        };

        private static IFormFile SeedFormFile(string fileName = "artigo.pdf", string contentType = "application/pdf", long length = 1024)
        {
            var fileMock = new Mock<IFormFile>();
            var bytes = Encoding.UTF8.GetBytes("conteudo-pdf");
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
            return fileMock.Object;
        }

        [Fact]
        public async Task GetEventDocumentsAsync_QuandoAlunoPertencerAGrupo_DeveRetornarDocumentosMapeados()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt, studentId);
            var doc = SeedDocument(group, studentId);
            group.Documents.Add(doc);
            _groupRepositoryMock.Setup(r => r.GetGroupForStudentInEventAsync(studentId, evt.Id)).ReturnsAsync(group);

            var result = await _eventDocumentsService.GetEventDocumentsAsync(evt.Id, studentId);

            Assert.Single(result);
            Assert.Equal(doc.Id, result[0].Id);
        }
        [Fact]
        public async Task GetEventDocumentsAsync_QuandoAlunoNaoPossuirGrupo_DeveRetornarListaVazia()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            _groupRepositoryMock.Setup(r => r.GetGroupForStudentInEventAsync(studentId, evt.Id)).ReturnsAsync((Group?)null!);

            var result = await _eventDocumentsService.GetEventDocumentsAsync(evt.Id, studentId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPendingApprovedAndReviewedDocuments_QuandoProfessorAutorizado_DeveRetornarListas()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };
            var group = SeedGroup(evt, Guid.NewGuid());
            var doc = SeedDocument(group, group.LeaderId);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _documentRepositoryMock.Setup(r => r.GetPendingByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });
            _documentRepositoryMock.Setup(r => r.GetApprovedByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });
            _documentRepositoryMock.Setup(r => r.GetReviewedByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });

            var pending = await _eventDocumentsService.GetPendingDocumentsForApprovalAsync(evt.Id, teacher.Id);
            var approved = await _eventDocumentsService.GetApprovedDocumentsByEventAsync(evt.Id, teacher.Id);
            var reviewed = await _eventDocumentsService.GetReviewedDocumentsByEventAsync(evt.Id, teacher.Id);

            Assert.Single(pending);
            Assert.Single(approved);
            Assert.Single(reviewed);
        }
        [Fact]
        public async Task GetPendingApprovedAndReviewedDocuments_QuandoProfessorNaoAutorizado_DeveLancarUnauthorizedAccessException()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = Guid.NewGuid() } } };
            var group = SeedGroup(evt, Guid.NewGuid());
            var doc = SeedDocument(group, group.LeaderId);

            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);
            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _documentRepositoryMock.Setup(r => r.GetPendingByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });
            _documentRepositoryMock.Setup(r => r.GetApprovedByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });
            _documentRepositoryMock.Setup(r => r.GetReviewedByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _eventDocumentsService.GetPendingDocumentsForApprovalAsync(evt.Id, teacher.Id));
        }

        [Fact]
        public async Task ReviewDocumentAsync_QuandoAprovarDocumento_DeveAtualizarStatusCriarHistoricoEInvalidarZip()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var group = SeedGroup(evt, Guid.NewGuid());
            var doc = SeedDocument(group, group.LeaderId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };

            _documentRepositoryMock.Setup(r => r.GetByIdAsync(doc.Id)).ReturnsAsync(doc);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(teacher.Id)).ReturnsAsync(teacher);

            var result = await _eventDocumentsService.ReviewDocumentAsync(doc.Id, teacher.Id, true, " Muito bom ");

            Assert.Equal(DocumentStatus.Approved, result.Status);
            Assert.Equal("Muito bom", result.Feedback);
            _documentRepositoryMock.Verify(r => r.UpdateAsync(doc), Times.Once);
            _documentRepositoryMock.Verify(r => r.AddApprovalAsync(It.Is<DocumentApprovalHistory>(h => h.Status == DocumentStatus.Approved)), Times.Once);
            _storageServiceMock.Verify(s => s.DeleteFileAsync($"events/{evt.Id}/compiled-documents.zip"), Times.Once);
        }
        [Fact]
        public async Task ReviewDocumentAsync_QuandoNaoExistir_DeveLancarKeyNotFoundException()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(courseId: courseId);
            var group = SeedGroup(evt, Guid.NewGuid());
            var doc = SeedDocument(group, group.LeaderId);
            var teacher = new Teacher { Id = Guid.NewGuid(), Courses = new List<Course> { new() { Id = courseId } } };
            _documentRepositoryMock.Setup(r => r.GetByIdAsync(doc.Id)).ReturnsAsync((Document)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _eventDocumentsService.ReviewDocumentAsync(doc.Id, teacher.Id, true, " Muito bom "));
        }

        [Fact]
        public async Task AddEventDocumentAsync_QuandoArquivoValido_DeveFazerUploadESalvarDocumento()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt, studentId);
            var file = SeedFormFile();

            _groupRepositoryMock.Setup(r => r.GetGroupForStudentInEventAsync(studentId, evt.Id)).ReturnsAsync(group);
            _storageServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), file.ContentType))
                .ReturnsAsync("groups/key/artigo.pdf");

            var result = await _eventDocumentsService.AddEventDocumentAsync(evt.Id, studentId, file);

            Assert.Equal("artigo.pdf", result.OriginalFileName);
            Assert.Equal(DocumentStatus.Pending, result.Status);
            _documentRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Document>()), Times.Once);
        }

        [Fact]
        public async Task AddEventDocumentAsync_QuandoFormatoInvalidoOuTamanhoExcedido_DeveLancarArgumentException()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt, studentId);
            _groupRepositoryMock.Setup(r => r.GetGroupForStudentInEventAsync(studentId, evt.Id)).ReturnsAsync(group);

            var invalidFormatFile = SeedFormFile("script.exe", "application/octet-stream");
            var oversizedFile = SeedFormFile("grande.pdf", "application/pdf", 20 * 1024 * 1024);

            await Assert.ThrowsAsync<ArgumentException>(() => _eventDocumentsService.AddEventDocumentAsync(evt.Id, studentId, invalidFormatFile));
            await Assert.ThrowsAsync<ArgumentException>(() => _eventDocumentsService.AddEventDocumentAsync(evt.Id, studentId, oversizedFile));
        }

        [Fact]
        public async Task DownloadDocumentAsync_QuandoMembroAceitoDoGrupo_DeveRetornarStreamDoArquivo()
        {
            var evt = SeedEvent();
            var studentId = Guid.NewGuid();
            var group = SeedGroup(evt, studentId);
            var doc = SeedDocument(group, studentId);
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("dados"));

            _documentRepositoryMock.Setup(r => r.GetByIdAsync(doc.Id)).ReturnsAsync(doc);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(new Student { Id = studentId });
            _storageServiceMock.Setup(s => s.GetFileStreamAsync(doc.StorageKey)).ReturnsAsync(expectedStream);

            var result = await _eventDocumentsService.DownloadDocumentAsync(doc.Id, studentId);

            Assert.Equal(expectedStream, result.FileStream);
            Assert.Equal(doc.OriginalFileName, result.FileName);
        }

        [Fact]
        public async Task DownloadCompiledDocumentsAsync_QuandoZipNaoExistirNoCache_DeveGerarZipFazerUploadERetornar()
        {
            var courseId = Guid.NewGuid();
            var evt = SeedEvent(startDate: DateTime.UtcNow.AddDays(-3), endDate: DateTime.UtcNow.AddDays(-1), courseId: courseId);
            var group = SeedGroup(evt, Guid.NewGuid());
            var doc1 = SeedDocument(group, group.LeaderId, DocumentStatus.Approved, "artigo.pdf");
            var doc2 = SeedDocument(group, group.LeaderId, DocumentStatus.Approved, "artigo.pdf");
            var admin = new Administrator { Id = Guid.NewGuid() };

            _eventRepositoryMock.Setup(r => r.GetByIdAsync(evt.Id)).ReturnsAsync(evt);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(admin.Id)).ReturnsAsync(admin);
            _documentRepositoryMock.Setup(r => r.GetApprovedByEventAsync(evt.Id)).ReturnsAsync(new List<Document> { doc1, doc2 });
            _storageServiceMock.Setup(s => s.GetFileStreamAsync($"events/{evt.Id}/compiled-documents.zip"))
                .ThrowsAsync(new FileNotFoundException());
            _storageServiceMock.Setup(s => s.GetFileStreamAsync(doc1.StorageKey))
                .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes("doc1")));

            var result = await _eventDocumentsService.DownloadCompiledDocumentsAsync(evt.Id, admin.Id);

            Assert.Equal("application/zip", result.ContentType);
            Assert.Contains("documentos-aprovados-", result.FileName);
            _storageServiceMock.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), $"events/{evt.Id}/compiled-documents.zip", "application/zip"), Times.Once);
        }

        [Fact]
        public async Task DeleteDocumentAsync_QuandoLiderOuAutorAntesDoPrazo_DeveExcluirDocumentoEInvalidarZipSeAprovado()
        {
            var evt = SeedEvent();
            var leaderId = Guid.NewGuid();
            var group = SeedGroup(evt, leaderId);
            var doc = SeedDocument(group, leaderId, DocumentStatus.Approved);
            _documentRepositoryMock.Setup(r => r.GetByIdAsync(doc.Id)).ReturnsAsync(doc);

            await _eventDocumentsService.DeleteDocumentAsync(doc.Id, leaderId);

            _storageServiceMock.Verify(s => s.DeleteFileAsync(doc.StorageKey), Times.Once);
            _documentRepositoryMock.Verify(r => r.DeleteAsync(doc), Times.Once);
            _storageServiceMock.Verify(s => s.DeleteFileAsync($"events/{evt.Id}/compiled-documents.zip"), Times.Once);
        }
    }
}

