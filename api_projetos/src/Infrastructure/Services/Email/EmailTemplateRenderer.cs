using System.Text;
using Microsoft.Extensions.Hosting;

namespace API_Gestao_Eventos.src.Infrastructure.Services.Email
{
    public class EmailTemplateRenderer(IHostEnvironment hostEnvironment)
    {
        private const string TemplatesRelativePath = "../Frontend/public/email-templates";

        public async Task<string> RenderAsync(string templateFileName, IDictionary<string, string> placeholders)
        {
            var fullTemplatesPath = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, TemplatesRelativePath));
            var templatePath = Path.Combine(fullTemplatesPath, templateFileName);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template de e-mail não encontrado no local: {templatePath}");

            var html = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);

            foreach (var placeholder in placeholders)
            {
                html = html.Replace("{{" + placeholder.Key + "}}", placeholder.Value ?? string.Empty, StringComparison.Ordinal);
            }

            return html;
        }
    }
}
