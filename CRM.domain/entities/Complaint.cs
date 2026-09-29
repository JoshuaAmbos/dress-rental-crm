namespace CRM.domain.entities;

public class Complaint
{
    public int ComplaintId { get; set; }
    public int CompanyId { get; set; }
    public int? BranchId { get; set; }

    public string ComplaintCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }

    public string Category { get; set; } = "General"; // Fit & Alterations, Garment Condition, Late Delivery, Customer Service, Billing Dispute, Deposit Deduction Issue
    public string Severity { get; set; } = "Medium";  // Low, Medium, High, Critical
    public string Status { get; set; } = "New";        // New, Under Investigation, In Progress, Resolved, Escalated

    public string Description { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
    public decimal CompensationAmount { get; set; } = 0.00m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Navigation property
    public Branch? Branch { get; set; }
}