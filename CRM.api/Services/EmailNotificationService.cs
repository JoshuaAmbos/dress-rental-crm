using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.api.Services;

public class EmailNotificationService
{
    private static readonly HttpClient _httpClient = new();
    private readonly Func<TenantCrmDbContext> _contextFactory;

    private static readonly string ApiToken =
        Environment.GetEnvironmentVariable("MAILTRAP_API_TOKEN") ?? "<YOUR_API_TOKEN>";

    private const string SenderEmail = "hello@demomailtrap.co";
    private const string SenderName = "Atelier Haute Couture";

    private static readonly string VerifiedTestInbox =
        Environment.GetEnvironmentVariable("MAILTRAP_TEST_INBOX") ?? "j.ambos.554298@umindanao.edu.ph";

    public EmailNotificationService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task SendNotificationAsync(
        int companyId,
        int? branchId,
        int? customerId,
        string recipientEmail,
        string clientName,
        string subject,
        string module,
        string messageHtml)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail)) return;

        bool isDummyDomain = recipientEmail.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase) ||
                             recipientEmail.EndsWith(".local", StringComparison.OrdinalIgnoreCase);

        string effectiveRecipient = isDummyDomain ? VerifiedTestInbox : recipientEmail;
        string effectiveSubject = isDummyDomain ? $"[SIMULATED - {clientName}] {subject}" : subject;
        string deliveryStatus = "Sent";

        string styledHtml = $@"
            <div style='font-family: Segoe UI, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #EADFD9; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #261618; color: #FFFFFF; padding: 20px 24px;'>
                    <h2 style='margin: 0; font-size: 20px; color: #FFFFFF;'>Atelier Haute Couture</h2>
                    <span style='font-size: 12px; color: #BE6E78; text-transform: uppercase;'>{module.ToUpperInvariant()} NOTIFICATION</span>
                </div>
                <div style='padding: 24px; background-color: #FFFFFF; color: #261618;'>
                    <p style='font-size: 15px;'>Dear <strong>{clientName}</strong>,</p>
                    <div style='line-height: 1.6; font-size: 14px;'>{messageHtml}</div>
                    <hr style='border: none; border-top: 1px solid #F0EAE8; margin: 20px 0;' />
                    <p style='font-size: 11px; color: #8C7C7E;'>Intended recipient: {recipientEmail} &bull; Active showroom concierge notification.</p>
                </div>
            </div>";

        // 1. Dispatch directly to Mailtrap REST API
        if (!string.IsNullOrWhiteSpace(ApiToken) && !ApiToken.Contains("YOUR_API_TOKEN"))
        {
            try
            {
                var payload = new
                {
                    to = new[]
                    {
                        new { email = effectiveRecipient, name = clientName }
                    },
                    from = new { email = SenderEmail, name = SenderName },
                    subject = effectiveSubject,
                    html = styledHtml,
                    category = module
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://send.api.mailtrap.io/api/send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    deliveryStatus = "Simulated";
                }
            }
            catch
            {
                // If offline or network drop occurs, prevent app crashes and log as Simulated
                deliveryStatus = "Simulated";
            }
        }
        else
        {
            deliveryStatus = "Simulated";
        }

        // 2. Audit persistence in DB_TenantCRM
        try
        {
            await using var db = _contextFactory();
            db.CustomerNotifications.Add(new CustomerNotification
            {
                CompanyId = companyId,
                BranchId = branchId,
                CustomerId = customerId,
                RecipientEmail = recipientEmail,
                Subject = subject,
                Body = messageHtml,
                Module = module,
                DeliveryStatus = deliveryStatus,
                SentAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch { }
    }
}