using SendGrid;
using SendGrid.Helpers.Mail;

namespace API_Gestao_Eventos.src.Infrastructure.Services.Email
{
    public class EmailService(IConfiguration configuration, HttpClient httpClient)
    {
        public async Task SendEmailAsync(EmailAddress to, string subject, string body)
        {
            var emailSettings = configuration.GetSection("Email");
            var apiKey = emailSettings["ApiKey"]
                ?? throw new InvalidOperationException("Chave de API de e-mail não configurada.");
            var fromEmail = emailSettings["From"]
                ?? throw new InvalidOperationException("Endereço de e-mail de remetente não configurado.");
            var fromName = emailSettings["FromName"] ?? "Gestão de Eventos";

            var from = new EmailAddress(fromEmail, fromName);
            var client = new SendGridClient(httpClient, new SendGridClientOptions
            {
                ApiKey = apiKey
            });

            var msg = MailHelper.CreateSingleEmail(
                from: from,
                to: to,
                subject: subject,
                plainTextContent: null,
                htmlContent: body
            );

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Falha ao enviar e-mail via SendGrid. Status: {response.StatusCode}. Resposta: {responseBody}"
                );
            }
        }
    }
}