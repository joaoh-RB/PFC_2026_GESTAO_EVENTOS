using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using API_Gestao_Eventos.src.Application.DTO.Event;
using API_Gestao_Eventos.src.Application.DTO.Group;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Controllers;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Domain.Enums;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static API_Gestao_Eventos.src.Controllers.EventsController;

namespace API_Gestao_Eventos.Tests.Integration
{
    public class EventsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private const string BasePath = "/api/events";
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public EventsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        #region Helpers

        private async Task<SeedData> SeedScenarioAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var institution = new Institution
            {
                Name = $"Faculdade Teste {Guid.NewGuid():N}",
                IsActive = true
            };
            await db.Institutions.AddAsync(institution);

            var course = new Course
            {
                Name = $"Curso Teste {Guid.NewGuid():N}",
                IsActive = true,
                InstitutionId = institution.Id,
                Institution = institution
            };
            await db.Courses.AddAsync(course);

            var admin = new Administrator
            {
                Name = "Admin Teste",
                Email = $"admin-{Guid.NewGuid():N}@teste.com",
                PasswordHash = "hash",
                InstitutionId = institution.Id,
                IsActive = true,
                ApprovalStatus = UserApprovalStatus.Aprovado
            };
            await db.Administrators.AddAsync(admin);

            var teacher = new Teacher
            {
                Name = "Professor Teste",
                Email = $"prof-{Guid.NewGuid():N}@teste.com",
                PasswordHash = "hash",
                InstitutionId = institution.Id,
                IsActive = true,
                ApprovalStatus = UserApprovalStatus.Aprovado,
                Courses = new List<Course> { course }
            };
            await db.Teachers.AddAsync(teacher);

            var student = new Student
            {
                Name = "Aluno Teste",
                Email = $"aluno-{Guid.NewGuid():N}@teste.com",
                PasswordHash = "hash",
                UniqueIdentifier = Guid.NewGuid().ToString("N")[..10],
                InstitutionId = institution.Id,
                CourseId = course.Id,
                IsActive = true,
                ApprovalStatus = UserApprovalStatus.Aprovado
            };
            await db.Students.AddAsync(student);

            await db.SaveChangesAsync();

            return new SeedData(institution, course, admin, teacher, student);
        }

        private HttpRequestMessage CreateAuthenticatedRequest(
            HttpMethod method,
            string url,
            Guid userId,
            UserRole role,
            object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Test-UserId", userId.ToString());
            request.Headers.Add("X-Test-Role", role.ToString());

            if (body != null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");
            }

            return request;
        }

        private sealed record SeedData(
            Institution Institution,
            Course Course,
            Administrator Admin,
            Teacher Teacher,
            Student Student);

        private sealed class CreatedIdResponse
        {
            public Guid Id { get; set; }
        }

        private sealed class MessageResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        private sealed class AttendanceCodeResponse
        {
            public string Code { get; set; } = string.Empty;
        }

        #endregion

        //CAMINHO FELIZ
        // POST /api/events + Verificação de header relevante (Location no 201 Created)
        [Fact]
        public async Task CreateEvent_QuandoCaminhoFeliz_DeveRetornarStatus201EHeaderLocation()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Palestra de Engenharia de Software",
                Description = "Descrição do evento de teste",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 100,
                AllowDocuments = true,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var request = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var created = await response.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal($"{BasePath}/{created.Id}", response.Headers.Location.ToString());
        }

        // PUT /api/events/{id} + Verificação de efeito persistido após escrita (via Repository e GET na API)
        [Fact]
        public async Task UpdateEvent_QuandoCaminhoFeliz_DeveRetornarStatus204EConfirmarEstadoPersistidoNoBancoEViaGetApi()
        {
            var seed = await SeedScenarioAsync();

            var createDto = new CreateEventRequestDto
            {
                Name = "Evento Original",
                Description = "Descrição original",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 50,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, createDto);
            var postResp = await _client.SendAsync(postReq);
            var created = await postResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            var updateDto = new CreateEventRequestDto
            {
                Name = "Evento Atualizado via PUT",
                Description = "Nova descrição atualizada",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(3),
                EndDate = DateTimeOffset.UtcNow.AddDays(4),
                Capacity = 120,
                AllowDocuments = true,
                EventType = EventType.Congresso,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var putReq = CreateAuthenticatedRequest(HttpMethod.Put, $"{BasePath}/{created.Id}", seed.Admin.Id, UserRole.Administrador, updateDto);
            var putResp = await _client.SendAsync(putReq);

            Assert.Equal(HttpStatusCode.NoContent, putResp.StatusCode);

            // Confirmação via Repository no banco de dados
            using (var scope = _factory.Services.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var persistedEvent = await repo.GetByIdAsync(created.Id);
                Assert.NotNull(persistedEvent);
                Assert.Equal("Evento Atualizado via PUT", persistedEvent.Name);
                Assert.Equal("Nova descrição atualizada", persistedEvent.Description);
                Assert.Equal(120, persistedEvent.Capacity);
                Assert.True(persistedEvent.AllowDocuments);
                Assert.Equal(EventType.Congresso, persistedEvent.EventType);
            }

            // Confirmação via GET na API
            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}?institutionId={seed.Institution.Id}", seed.Admin.Id, UserRole.Administrador);
            var getResp = await _client.SendAsync(getReq);
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            var paged = await getResp.Content.ReadFromJsonAsync<PagedResponseDto<EventResponseDto>>(_jsonOptions);
            Assert.NotNull(paged);
            Assert.Contains(paged.Items, e => e.Id == created.Id && e.Name == "Evento Atualizado via PUT" && e.Capacity == 120);
        }

        // GET /api/events
        [Fact]
        public async Task GetEvents_QuandoCaminhoFeliz_DeveRetornarStatus200EEventosPaginados()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Listagem Geral",
                Description = "Descrição do evento",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 80,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            await _client.SendAsync(postReq);

            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}?institutionId={seed.Institution.Id}", seed.Student.Id, UserRole.Aluno);
            var response = await _client.SendAsync(getReq);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paged = await response.Content.ReadFromJsonAsync<PagedResponseDto<EventResponseDto>>(_jsonOptions);
            Assert.NotNull(paged);
            Assert.Contains(paged.Items, e => e.Name == "Evento Listagem Geral");
        }

        // GET /api/events/requested
        [Fact]
        public async Task GetRequestedEvents_QuandoAlunoSolicitaEvento_DeveRetornarStatus200EEventoSolicitado()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Solicitado por Aluno",
                Description = "Proposta de evento acadêmico",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                Capacity = 40,
                AllowDocuments = false,
                EventType = EventType.Congresso,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            // Aluno cria evento -> entra como Solicitado
            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Student.Id, UserRole.Aluno, dto);
            var postResp = await _client.SendAsync(postReq);
            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);

            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/requested?institutionId={seed.Institution.Id}", seed.Admin.Id, UserRole.Administrador);
            var getResp = await _client.SendAsync(getReq);

            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var paged = await getResp.Content.ReadFromJsonAsync<PagedResponseDto<EventResponseDto>>(_jsonOptions);
            Assert.NotNull(paged);
            Assert.Contains(paged.Items, e => e.Name == "Evento Solicitado por Aluno" && e.EventStatus == (int)EventStatus.Solicitado);
        }

        // PATCH /api/events/{id}/approval
        [Fact]
        public async Task SetApproval_QuandoEventoSolicitado_DeveRetornarStatus200EAprovarEvento()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Para Aprovar",
                Description = "Aguardando aprovação",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                Capacity = 60,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Student.Id, UserRole.Aluno, dto);
            var postResp = await _client.SendAsync(postReq);
            var created = await postResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            var approvalBody = new ApprovalEventRequest { Status = EventStatus.Agendado };
            using var patchReq = CreateAuthenticatedRequest(HttpMethod.Patch, $"{BasePath}/{created.Id}/approval", seed.Admin.Id, UserRole.Administrador, approvalBody);
            var patchResp = await _client.SendAsync(patchReq);

            Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);
            var body = await patchResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(body);
            Assert.Equal("Evento aprovado.", body.Message);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedEvent = await db.Events.FindAsync(created.Id);
            Assert.NotNull(updatedEvent);
            Assert.Equal(EventStatus.Agendado, updatedEvent.EventStatus);
        }

        // PATCH /api/events/{id}/active
        [Fact]
        public async Task SetActive_QuandoEventoFuturo_DeveRetornarStatus200EInativarEvento()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Para Inativar",
                Description = "Teste de inativação",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                Capacity = 50,
                AllowDocuments = false,
                EventType = EventType.Simpósio,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var postResp = await _client.SendAsync(postReq);
            var created = await postResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            var activeBody = new ActiveRequest { IsActive = false };
            using var patchReq = CreateAuthenticatedRequest(HttpMethod.Patch, $"{BasePath}/{created.Id}/active", seed.Admin.Id, UserRole.Administrador, activeBody);
            var patchResp = await _client.SendAsync(patchReq);

            Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);
            var msg = await patchResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Evento inativado.", msg.Message);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var evt = await db.Events.FindAsync(created.Id);
            Assert.NotNull(evt);
            Assert.False(evt.IsActive);
        }

        // GET /api/events/types
        [Fact]
        public async Task GetEventTypes_QuandoCaminhoFeliz_DeveRetornarStatus200EListaDeTipos()
        {
            var response = await _client.GetAsync($"{BasePath}/types");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var types = await response.Content.ReadFromJsonAsync<List<SelectItemDto>>(_jsonOptions);
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.Contains(types, t => t.Label == nameof(EventType.Palestra));
        }

        // POST /api/events/{id}/add-to-calendar
        [Fact]
        public async Task AddToCalendar_QuandoEventoIntegrado_DeveRetornarStatus200ESalvarNaAgenda()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Google Calendar",
                Description = "Teste de agenda",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 50,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var createResp = await _client.SendAsync(createReq);
            var created = await createResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            using var addCalReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/add-to-calendar", seed.Student.Id, UserRole.Aluno);
            var addCalResp = await _client.SendAsync(addCalReq);

            Assert.Equal(HttpStatusCode.OK, addCalResp.StatusCode);
            var msg = await addCalResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Evento adicionado à sua agenda do Google com sucesso!", msg.Message);
        }

        // POST /api/events/{id}/remove-from-calendar
        [Fact]
        public async Task RemoveFromCalendar_QuandoEventoNaAgenda_DeveRetornarStatus200ERemoverDaAgenda()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Remover Calendar",
                Description = "Teste remover da agenda",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 50,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var createResp = await _client.SendAsync(createReq);
            var created = await createResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            using var addCalReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/add-to-calendar", seed.Student.Id, UserRole.Aluno);
            await _client.SendAsync(addCalReq);

            using var remCalReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/remove-from-calendar", seed.Student.Id, UserRole.Aluno);
            var remCalResp = await _client.SendAsync(remCalReq);

            Assert.Equal(HttpStatusCode.OK, remCalResp.StatusCode);
            var msg = await remCalResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Evento removido da sua agenda do Google com sucesso!", msg.Message);
        }

        // POST /api/events/{id}/join
        [Fact]
        public async Task JoinEvent_QuandoAlunoElegivel_DeveRetornarStatus200EConfirmarInscricao()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Para Inscrição",
                Description = "Inscrição aberta",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 30,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var createResp = await _client.SendAsync(createReq);
            var created = await createResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            using var joinReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/join", seed.Student.Id, UserRole.Aluno);
            var joinResp = await _client.SendAsync(joinReq);

            Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);
            var msg = await joinResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Você se inscreveu no evento com sucesso!", msg.Message);
        }

        // POST /api/events/{id}/leave
        [Fact]
        public async Task LeaveEvent_QuandoAlunoInscrito_DeveRetornarStatus200ECancelarInscricao()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Para Sair",
                Description = "Teste cancelar inscrição",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 30,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var createResp = await _client.SendAsync(createReq);
            var created = await createResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            using var joinReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/join", seed.Student.Id, UserRole.Aluno);
            await _client.SendAsync(joinReq);

            using var leaveReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/leave", seed.Student.Id, UserRole.Aluno);
            var leaveResp = await _client.SendAsync(leaveReq);

            Assert.Equal(HttpStatusCode.OK, leaveResp.StatusCode);
            var msg = await leaveResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Você saiu do evento com sucesso!", msg.Message);
        }

        // GET /api/events/{id}/registrations
        [Fact]
        public async Task GetRegistrations_QuandoHouverAlunoInscrito_DeveRetornarStatus200EListaDeInscritos()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Com Inscritos",
                Description = "Lista de inscritos",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 30,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var createResp = await _client.SendAsync(createReq);
            var created = await createResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(created);

            using var joinReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{created.Id}/join", seed.Student.Id, UserRole.Aluno);
            await _client.SendAsync(joinReq);

            using var regReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{created.Id}/registrations", seed.Admin.Id, UserRole.Administrador);
            var regResp = await _client.SendAsync(regReq);

            Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);
            var paged = await regResp.Content.ReadFromJsonAsync<PagedResponseDto<EventRegistrationDto>>(_jsonOptions);
            Assert.NotNull(paged);
            Assert.Single(paged.Items);
            Assert.Contains(paged.Items, r => r.StudentId == seed.Student.Id && r.StudentName == seed.Student.Name);
        }

        // GET /api/events/{id}/attendance/code
        [Fact]
        public async Task GetAttendanceCode_QuandoEventoIniciado_DeveRetornarStatus200ECodigoDePresenca()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Em Andamento Código",
                    Description = "Teste código de presença",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddMinutes(-30),
                    EndDate = DateTime.UtcNow.AddHours(2),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var codeReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{eventId}/attendance/code", seed.Admin.Id, UserRole.Administrador);
            var codeResp = await _client.SendAsync(codeReq);

            Assert.Equal(HttpStatusCode.OK, codeResp.StatusCode);
            var result = await codeResp.Content.ReadFromJsonAsync<AttendanceCodeResponse>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(6, result.Code.Length);
        }

        // GET /api/events/{id}/attendance
        [Fact]
        public async Task GetAttendance_QuandoCaminhoFeliz_DeveRetornarStatus200EListaDePresencas()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Lista Presença",
                    Description = "Teste lista de presença",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddMinutes(-30),
                    EndDate = DateTime.UtcNow.AddHours(2),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! },
                    EventStudents = new List<EventStudent>
                    {
                        new()
                        {
                            StudentId = seed.Student.Id,
                            Status = RegistrationStatus.Confirmed,
                            RegisteredAt = DateTime.UtcNow.AddHours(-1),
                            AttendedAt = DateTime.UtcNow.AddMinutes(-10),
                            AttendanceMethod = AttendanceMethod.Manual
                        }
                    }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var attReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{eventId}/attendance", seed.Admin.Id, UserRole.Administrador);
            var attResp = await _client.SendAsync(attReq);

            Assert.Equal(HttpStatusCode.OK, attResp.StatusCode);
            var items = await attResp.Content.ReadFromJsonAsync<List<AttendanceRegistrationDto>>(_jsonOptions);
            Assert.NotNull(items);
            Assert.Single(items);
            Assert.True(items[0].Attended);
            Assert.Equal(seed.Student.Id, items[0].StudentId);
        }

        // POST /api/events/{id}/attendance/confirm
        [Fact]
        public async Task ConfirmAttendance_QuandoCodigoValido_DeveRetornarStatus200EConfirmarPresenca()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Confirmar Presença Código",
                    Description = "Teste confirmação por código",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddMinutes(-20),
                    EndDate = DateTime.UtcNow.AddHours(2),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AttendanceCode = "ABC234",
                    AllowedCourses = new List<Course> { course! },
                    EventStudents = new List<EventStudent>
                    {
                        new()
                        {
                            StudentId = seed.Student.Id,
                            Status = RegistrationStatus.Confirmed,
                            RegisteredAt = DateTime.UtcNow.AddHours(-1)
                        }
                    }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            var confirmDto = new ConfirmAttendanceRequestDto { Code = "ABC234" };
            using var confirmReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{eventId}/attendance/confirm", seed.Student.Id, UserRole.Aluno, confirmDto);
            var confirmResp = await _client.SendAsync(confirmReq);

            Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
            var msg = await confirmResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Presença confirmada.", msg.Message);
        }

        // POST /api/events/{id}/attendance/{studentId}
        [Fact]
        public async Task MarkAttendance_QuandoEventoIniciado_DeveRetornarStatus200ERegistrarPresencaManual()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Marcar Presença Manual",
                    Description = "Teste presença manual",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddMinutes(-20),
                    EndDate = DateTime.UtcNow.AddHours(2),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! },
                    EventStudents = new List<EventStudent>
                    {
                        new()
                        {
                            StudentId = seed.Student.Id,
                            Status = RegistrationStatus.Confirmed,
                            RegisteredAt = DateTime.UtcNow.AddHours(-1)
                        }
                    }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var markReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{eventId}/attendance/{seed.Student.Id}", seed.Admin.Id, UserRole.Administrador);
            var markResp = await _client.SendAsync(markReq);

            Assert.Equal(HttpStatusCode.OK, markResp.StatusCode);
            var msg = await markResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Presença registrada.", msg.Message);
        }

        // DELETE /api/events/{id}/attendance/{studentId}
        [Fact]
        public async Task UnmarkAttendance_QuandoPresencaMarcada_DeveRetornarStatus200ERemoverPresencaNoBanco()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Desmarcar Presença",
                    Description = "Teste remover presença",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddMinutes(-20),
                    EndDate = DateTime.UtcNow.AddHours(2),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! },
                    EventStudents = new List<EventStudent>
                    {
                        new()
                        {
                            StudentId = seed.Student.Id,
                            Status = RegistrationStatus.Confirmed,
                            RegisteredAt = DateTime.UtcNow.AddHours(-1),
                            AttendedAt = DateTime.UtcNow.AddMinutes(-5),
                            AttendanceMethod = AttendanceMethod.Manual
                        }
                    }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var delReq = CreateAuthenticatedRequest(HttpMethod.Delete, $"{BasePath}/{eventId}/attendance/{seed.Student.Id}", seed.Admin.Id, UserRole.Administrador);
            var delResp = await _client.SendAsync(delReq);

            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
            var msg = await delResp.Content.ReadFromJsonAsync<MessageResponse>(_jsonOptions);
            Assert.NotNull(msg);
            Assert.Equal("Presença removida.", msg.Message);

            // Verifica efeito persistido após DELETE no banco via Repository
            using (var scope = _factory.Services.CreateScope())
            {
                var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                var registration = await attendanceRepo.GetRegistrationAsync(eventId, seed.Student.Id);
                Assert.NotNull(registration);
                Assert.Null(registration.AttendedAt);
                Assert.Null(registration.AttendanceMethod);
            }
        }

        // POST /api/events/{id}/feedback
        [Fact]
        public async Task CreateFeedback_QuandoEventoEncerradoEComPresenca_DeveRetornarStatus201ECriarAvaliacao()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Encerrado Para Feedback",
                    Description = "Teste criação de feedback",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddHours(-4),
                    EndDate = DateTime.UtcNow.AddHours(-1),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! },
                    EventStudents = new List<EventStudent>
                    {
                        new()
                        {
                            StudentId = seed.Student.Id,
                            Status = RegistrationStatus.Confirmed,
                            RegisteredAt = DateTime.UtcNow.AddHours(-5),
                            AttendedAt = DateTime.UtcNow.AddHours(-3),
                            AttendanceMethod = AttendanceMethod.Code
                        }
                    }
                };
                await db.Events.AddAsync(evt);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            var feedbackDto = new CreateEventFeedbackRequestDto
            {
                Rating = 10,
                FeedbackText = "Ótimo evento acadêmico!"
            };

            using var postReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{eventId}/feedback", seed.Student.Id, UserRole.Aluno, feedbackDto);
            var postResp = await _client.SendAsync(postReq);

            Assert.Equal(HttpStatusCode.Created, postResp.StatusCode);
            var createdFeedback = await postResp.Content.ReadFromJsonAsync<EventFeedbackResponseDto>(_jsonOptions);
            Assert.NotNull(createdFeedback);
            Assert.Equal(10, createdFeedback.Rating);
            Assert.Equal("Ótimo evento acadêmico!", createdFeedback.FeedbackText);
            Assert.Equal(seed.Student.Id, createdFeedback.StudentId);
        }

        // GET /api/events/{id}/feedback/mine
        [Fact]
        public async Task GetMyFeedback_QuandoAvaliacaoExistir_DeveRetornarStatus200EAvaliacaoDoAluno()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Meu Feedback",
                    Description = "Teste consulta de meu feedback",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddHours(-4),
                    EndDate = DateTime.UtcNow.AddHours(-1),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! }
                };
                await db.Events.AddAsync(evt);

                var feedback = new EventFeedback
                {
                    EventId = evt.Id,
                    StudentId = seed.Student.Id,
                    Rating = 9,
                    FeedbackText = "Muito instrutivo",
                    CreatedAt = DateTime.UtcNow
                };
                await db.EventFeedbacks.AddAsync(feedback);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{eventId}/feedback/mine", seed.Student.Id, UserRole.Aluno);
            var getResp = await _client.SendAsync(getReq);

            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var result = await getResp.Content.ReadFromJsonAsync<EventFeedbackResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(9, result.Rating);
            Assert.Equal("Muito instrutivo", result.FeedbackText);
        }

        // GET /api/events/{id}/feedbacks
        [Fact]
        public async Task GetEventFeedbacks_QuandoCaminhoFeliz_DeveRetornarStatus200EListaDeFeedbacksDoEvento()
        {
            var seed = await SeedScenarioAsync();
            Guid eventId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var course = await db.Courses.FindAsync(seed.Course.Id);
                var evt = new Event
                {
                    Name = "Evento Todos Feedbacks",
                    Description = "Teste listagem de feedbacks",
                    InstitutionId = seed.Institution.Id,
                    StartDate = DateTime.UtcNow.AddHours(-4),
                    EndDate = DateTime.UtcNow.AddHours(-1),
                    Capacity = 50,
                    IsActive = true,
                    EventStatus = EventStatus.Agendado,
                    EventType = EventType.Palestra,
                    AllowedCourses = new List<Course> { course! }
                };
                await db.Events.AddAsync(evt);

                var feedback = new EventFeedback
                {
                    EventId = evt.Id,
                    StudentId = seed.Student.Id,
                    Rating = 8,
                    FeedbackText = "Feedback listado para gestão",
                    CreatedAt = DateTime.UtcNow
                };
                await db.EventFeedbacks.AddAsync(feedback);
                await db.SaveChangesAsync();
                eventId = evt.Id;
            }

            using var getReq = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/{eventId}/feedbacks", seed.Admin.Id, UserRole.Administrador);
            var getResp = await _client.SendAsync(getReq);

            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var feedbacks = await getResp.Content.ReadFromJsonAsync<List<EventFeedbackResponseDto>>(_jsonOptions);
            Assert.NotNull(feedbacks);
            Assert.Single(feedbacks);
            Assert.Equal("Feedback listado para gestão", feedbacks[0].FeedbackText);
        }

        //Erros
        // Erro 400 BadRequest (regra de negócio / curso inválido para a instituição)
        [Fact]
        public async Task CreateEvent_QuandoCursoNaoPertenceAInstituicao_DeveRetornarStatus400ECorpoDeErro()
        {
            var seed = await SeedScenarioAsync();

            var dto = new CreateEventRequestDto
            {
                Name = "Evento Com Curso Inválido",
                Description = "Curso de outra instituição",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(2),
                Capacity = 50,
                AllowDocuments = false,
                EventType = EventType.Palestra,
                AllowedCourses = new List<Guid> { Guid.NewGuid() }
            };

            using var request = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, dto);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Nem todos os cursos cadastrados são permitidos.", errorBody);
        }

        // Erro 404 NotFound (recurso inexistente ao aprovar evento)
        [Fact]
        public async Task SetApproval_QuandoEventoNaoEncontrado_DeveRetornarStatus404ECorpoDeErro()
        {
            var seed = await SeedScenarioAsync();
            var idInexistente = Guid.NewGuid();

            var approvalBody = new ApprovalEventRequest { Status = EventStatus.Agendado };
            using var request = CreateAuthenticatedRequest(HttpMethod.Patch, $"{BasePath}/{idInexistente}/approval", seed.Admin.Id, UserRole.Administrador, approvalBody);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var errorBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Evento não encontrado.", errorBody);
        }


        //Cruzar recurso
        // Criar evento -> Inscrever aluno no evento -> Criar grupo no evento -> Listar grupos daquele evento
        [Fact]
        public async Task FluxoCruzado_CriarEventoInscreverAlunoCriarGrupoEListarGruposDoEvento_DeveRetornarGrupoVinculado()
        {
            var seed = await SeedScenarioAsync();

            // 1. Criar evento permitindo documentos (necessário para criação de grupos)
            var createEventDto = new CreateEventRequestDto
            {
                Name = "Feira de Projetos Integrados",
                Description = "Evento com submissão de trabalhos em grupo",
                InstitutionId = seed.Institution.Id,
                StartDate = DateTimeOffset.UtcNow.AddDays(2),
                EndDate = DateTimeOffset.UtcNow.AddDays(3),
                Capacity = 40,
                AllowDocuments = true,
                EventType = EventType.Congresso,
                AllowedCourses = new List<Guid> { seed.Course.Id }
            };

            using var createEventReq = CreateAuthenticatedRequest(HttpMethod.Post, BasePath, seed.Admin.Id, UserRole.Administrador, createEventDto);
            var createEventResp = await _client.SendAsync(createEventReq);
            Assert.Equal(HttpStatusCode.Created, createEventResp.StatusCode);

            var createdEvent = await createEventResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(createdEvent);

            // 2. Inscrever o aluno no evento
            using var joinReq = CreateAuthenticatedRequest(HttpMethod.Post, $"{BasePath}/{createdEvent.Id}/join", seed.Student.Id, UserRole.Aluno);
            var joinResp = await _client.SendAsync(joinReq);
            Assert.Equal(HttpStatusCode.OK, joinResp.StatusCode);

            // 3. Criar um grupo vinculado ao evento criado (recurso GroupsController: POST /api/groups)
            var createGroupDto = new CreateGroupRequestDto
            {
                EventId = createdEvent.Id,
                StudentIds = new List<Guid>()
            };

            using var createGroupReq = CreateAuthenticatedRequest(HttpMethod.Post, "/api/groups", seed.Student.Id, UserRole.Aluno, createGroupDto);
            var createGroupResp = await _client.SendAsync(createGroupReq);
            Assert.Equal(HttpStatusCode.Created, createGroupResp.StatusCode);

            var createdGroup = await createGroupResp.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(createdGroup);
            Assert.NotEqual(Guid.Empty, createdGroup.Id);

            // 4. Listar os grupos daquele evento (GET /api/groups/event/{eventId})
            using var listGroupsReq = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/groups/event/{createdEvent.Id}", seed.Admin.Id, UserRole.Administrador);
            var listGroupsResp = await _client.SendAsync(listGroupsReq);
            Assert.Equal(HttpStatusCode.OK, listGroupsResp.StatusCode);

            var groups = await listGroupsResp.Content.ReadFromJsonAsync<List<GroupResponseDto>>(_jsonOptions);
            Assert.NotNull(groups);
            Assert.Single(groups);
            Assert.Equal(createdGroup.Id, groups[0].Id);
            Assert.Equal(createdEvent.Id, groups[0].EventId);
            Assert.Equal(seed.Student.Id, groups[0].LeaderId);
        }


        //Segurança
        [Fact]
        public async Task Seguranca_GetRequestedEvents_SemToken_DeveRetornarStatus401()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BasePath}/requested");
            request.Headers.Add("X-Anonymous", "true");

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Seguranca_GetRequestedEvents_UsuarioSemPermissao_DeveRetornarStatus403()
        {
            var seed = await SeedScenarioAsync();

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/requested", seed.Student.Id, UserRole.Aluno);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Seguranca_GetRequestedEvents_UsuarioComPermissao_DeveRetornarStatus200()
        {
            var seed = await SeedScenarioAsync();

            using var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{BasePath}/requested", seed.Admin.Id, UserRole.Administrador);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
