using System.Net.Http.Headers;
namespace API_Gestao_Eventos.src.Infrastructure.Services.Storage
{
    public class SupabaseStorageService : IStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _bucketName;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SupabaseStorageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var config = configuration.GetSection("Supabase");
            var url = config["Url"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Supabase:Url não configurado.");
            var serviceKey = config["ServiceKey"]
                ?? throw new InvalidOperationException("Supabase:ServiceKey não configurado.");

            _bucketName = config["BucketName"] ?? "documents";

            _httpClient.BaseAddress = new Uri($"{url}/storage/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", serviceKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", serviceKey);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var requestUri = $"object/{_bucketName}/{fileName}";

            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _httpClient.PostAsync(requestUri, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao fazer upload no Supabase Storage: {Error}", errorBody);
                throw new HttpRequestException($"Falha no upload do arquivo para o Supabase: {response.StatusCode} - {errorBody}");
            }

            return fileName;
        }

        public async Task<Stream> GetFileStreamAsync(string storageKey)
        {
            // GET no objeto autenticado para trazer o stream binário
            var requestUri = $"object/authenticated/{_bucketName}/{storageKey}";

            var response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro ao recuperar arquivo {Key} do Supabase: {Status}", storageKey, response.StatusCode);
                throw new FileNotFoundException($"Arquivo não encontrado no Supabase: {storageKey}");
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task DeleteFileAsync(string storageKey)
        {
            var requestUri = $"object/{_bucketName}/{storageKey}";

            var response = await _httpClient.DeleteAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Falha ao deletar arquivo {Key} do Supabase: {Error}", storageKey, errorBody);
            }
        }
    }
}