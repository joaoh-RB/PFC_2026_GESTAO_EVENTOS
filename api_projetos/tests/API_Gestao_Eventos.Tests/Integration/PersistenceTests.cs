using API_Gestao_Eventos.src.Domain.Entities;
using API_Gestao_Eventos.src.Infrastructure.Data.Context;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace API_Gestao_Eventos.Tests.Integration
{
    public class PersistenceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public PersistenceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new AppDbContext(_options, null);
            context.Database.EnsureCreated();
        }

        [Fact]
        public async Task InstitutionRepository_DeveSalvarERecuperarEConsultarCustomizado()
        {
            using var context = new AppDbContext(_options, null);
            var repository = new InstitutionRepository(context);

            var institution = new Institution
            {
                Name = "Instituto de Tecnologia de Teste",
                Address = "Rua Teste, 123",
                Phone = "11999999999",
                IsActive = true
            };

            await repository.AddAsync(institution);

            var retrieved = await repository.GetByIdAsync(institution.Id);
            Assert.NotNull(retrieved);
            Assert.Equal(institution.Name, retrieved.Name);

            var exists = await repository.ExistsByNameAsync("Instituto de Tecnologia de Teste");
            Assert.True(exists);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
