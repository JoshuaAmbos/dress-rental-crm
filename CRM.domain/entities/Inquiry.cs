namespace CRM.domain.entities;

public class Inquiry
{
    public int InquiryId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; } // Showroom Branch Scope
    public string InquiryCode { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string GarmentRequest { get; set; } = string.Empty;
    public string BudgetRange { get; set; } = "₱3,000–₱5,000";

    public string Priority { get; set; } = "Medium"; // Low, Medium, High
    public string Status { get; set; } = "New";        // New, In Review, Quoted, Converted, Closed
    public string? InquiryType { get; set; } = "Rental Request"; // Rental Request, Consultation
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Branch? Branch { get; set; }
}