using API_Gestao_Eventos.src.Application.DTO.Utils;
using API_Gestao_Eventos.src.Infrastructure.Data.Repositories;
using API_Gestao_Eventos.src.Application.DTO.Institution;

namespace API_Gestao_Eventos.src.Application.Services
{
    public class InstitutionService(InstitutionRepository institutionRepository)
    {
        private readonly InstitutionRepository _institutionRepository = institutionRepository;

        public async Task<IEnumerable<SelectItemDto>> GetInstituctionsForSelectAsync()
        {
            var institutions = await _institutionRepository.GetAllAsync();
            return institutions.Select(i => new SelectItemDto
                {
                    Value = i.Id.ToString(),
                    Label = i.Name
                }); 
        }
        public async Task<Guid> CreateInstitutionAsync(CreateInstitutionDto request)
        {
            var nameExists = await _institutionRepository.ExistsByNameAsync(request.Name);
            if (nameExists)
                throw new InvalidOperationException("Instituição já cadastrada.");

            Domain.Cnpj? cnpj = null;
            if (!string.IsNullOrWhiteSpace(request.Cnpj))
            {
                cnpj = Domain.Cnpj.Create(request.Cnpj);    
                var cnpjExists = await _institutionRepository.ExistsByCnpjAsync(cnpj);
                if (cnpjExists)
                    throw new InvalidOperationException("CNPJ já cadastrado para outra instituição.");
            }

            var institution = new Domain.Entities.Institution
            {
                Name = request.Name,
                Cnpj = cnpj,
                Address = request.Address,
                Phone = request.Phone
            };

            await _institutionRepository.AddAsync(institution);
            return institution.Id;
        }
    }
}
