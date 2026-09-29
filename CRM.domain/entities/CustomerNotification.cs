using System.ComponentModel.DataAnnotations;

namespace CRM.domain.entities;

public class CustomerNotification
{
    [Key]
    public int NotificationId { get; set; }

    public int CompanyId { get; set; }
    public int? BranchId { get; set; }
    public int? CustomerId { get; set; }

    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Module { get; set; } = "General";
    public string DeliveryStatus { get; set; } = "Sent";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Branch? Branch { get; set; }
    public Customer? Customer { get; set; }
}