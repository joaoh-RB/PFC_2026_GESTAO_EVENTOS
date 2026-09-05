using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class InstitutionService(InstitutionRepository institutionRepository)
    {
        private readonly InstitutionRepository _institutionRepository = institutionRepository;

        public async Task<IEnumerable<SelectItemDto>> GetInstituctionsForSelectAsync()
        {
            var institutions = await _institutionRepository.GetAllAsync();

            return institutions
                .Select(i => new SelectItemDto
                {
                    Value = i.Id.ToString(),
                    Label = i.Id.ToString()
                });
        }
    }
}
