namespace API_Gestao_Eventos.src.Infrastructure.Services.Storage
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> GetFileStreamAsync(string storageKey);
        Task DeleteFileAsync(string storageKey);
    }
}