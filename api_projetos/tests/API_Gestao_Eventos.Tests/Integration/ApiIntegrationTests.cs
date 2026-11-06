using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API_Gestao_Eventos.src.Application.DTO.Institution;
using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace API_Gestao_Eventos.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // 1. Teste de endpoint da API com caminho feliz, verificando status HTTP e corpo da resposta
        [Fact]
        public async Task GetInstitutions_QuandoCaminhoFeliz_DeveRetornarStatus200ECorpoComInstituicoes()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var institution = new Institution
                {
                    Name = "Faculdade Endpoint Feliz",
                    IsActive = true
                };
                await db.Institutions.AddAsync(institution);
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/institutions");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<SelectItemDto>>(content, _jsonOptions);

            Assert.NotNull(items);
            Assert.NotEmpty(items);
            Assert.Contains(items, i => i.Label == "Faculdade Endpoint Feliz");
        }

        // 2. Teste de endpoint com erro (4xx) verificando status e corpo de erro
        [Fact]
        public async Task GetInstitutionById_QuandoNaoEncontrado_DeveRetornarStatus404ECorpoDeErro()
        {
            // Arrange
            var idInexistente = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/institutions/{idInexistente}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Instituição não encontrada.", errorContent);
        }

        // 4. Fluxo completo que cruza API, service e banco (criar e depois consultar o recurso)
        [Fact]
        public async Task FluxoCompleto_CriarEConsultarInstituicao_DeveCruzarApiComServiceEBanco()
        {
            // Arrange
            var createRequest = new CreateInstitutionDto
            {
                Name = "Instituto Fluxo Completo E2E",
                Address = "Av. Brasil, 500",
                Phone = "11988887777"
            };

            // Act
            var postResponse = await _client.PostAsJsonAsync("/api/institutions", createRequest);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var postResult = await postResponse.Content.ReadFromJsonAsync<CreatedIdResponse>(_jsonOptions);
            Assert.NotNull(postResult);
            Assert.NotEqual(Guid.Empty, postResult.Id);

            var getResponse = await _client.GetAsync($"/api/institutions/{postResult.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var institution = await getResponse.Content.ReadFromJsonAsync<InstitutionDetailDto>(_jsonOptions);

            Assert.NotNull(institution);
            Assert.Equal(postResult.Id, institution.Id);
            Assert.Equal(createRequest.Name, institution.Name);
            Assert.Equal(createRequest.Address, institution.Address);
            Assert.Equal(createRequest.Phone, institution.Phone);
            Assert.True(institution.IsActive);
        }

        private class CreatedIdResponse
        {
            public Guid Id { get; set; }
        }
    }
}
