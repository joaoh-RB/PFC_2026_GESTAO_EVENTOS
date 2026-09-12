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
        public async Task<InstitutionDetailDto?> GetByIdAsync(
            Guid id,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isSecretary)
        {
            var isAllowed = isAdmin || (isSecretary && callerInstitutionId == id);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para consultar esta instituição.");

            var institution = await _institutionRepository.GetByIdAsync(id);
            if (institution == null) return null;

            return new InstitutionDetailDto
            {
                Id = institution.Id,
                Name = institution.Name,
                Cnpj = institution.Cnpj?.ToString(),
                Address = institution.Address,
                Phone = institution.Phone,
                IsActive = institution.IsActive,
                CreatedAt = institution.CreatedAt
            };
        }
        public async Task UpdateInstitutionAsync(
            Guid id,
            UpdateInstitutionDto request,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isSecretary)
        {
            var isAllowed = isAdmin || (isSecretary && callerInstitutionId == id);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para editar esta instituição.");

            var institution = await _institutionRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Instituição não encontrada.");

            var nameExists = await _institutionRepository.ExistsByNameExceptAsync(request.Name, id);
            if (nameExists)
                throw new InvalidOperationException("Já existe outra instituição com este nome.");

            Domain.Cnpj? cnpj = null;
            if (!string.IsNullOrWhiteSpace(request.Cnpj))
            {
                cnpj = Domain.Cnpj.Create(request.Cnpj);
                var cnpjExists = await _institutionRepository.ExistsByCnpjExceptAsync(cnpj, id);
                if (cnpjExists)
                    throw new InvalidOperationException("CNPJ já cadastrado para outra instituição.");
            }

            institution.Name = request.Name;
            institution.Cnpj = cnpj;
            institution.Address = request.Address;
            institution.Phone = request.Phone;

            await _institutionRepository.UpdateAsync(institution);
        }
        public async Task DeleteInstitutionAsync(
            Guid id,
            bool isAdmin,
            Guid? callerInstitutionId,
            bool isSecretary)
        {
            var isAllowed = isAdmin || (isSecretary && callerInstitutionId == id);
            if (!isAllowed)
                throw new UnauthorizedAccessException("Você não tem permissão para excluir esta instituição.");

            var institution = await _institutionRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Instituição não encontrada.");

            if (await _institutionRepository.HasDependenciesAsync(id))
                throw new InvalidOperationException("Não é possível excluir: existem usuários, cursos ou eventos vinculados a esta instituição.");

            await _institutionRepository.DeleteAsync(institution);
        }
    }
}