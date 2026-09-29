namespace CRM.api.DTOs;

public class ComplaintRowViewModel
{
    public int ComplaintId { get; set; }
    public string ComplaintCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "New";
    public string Description { get; set; } = string.Empty;
    public decimal CompensationAmount { get; set; }
    public string BranchName { get; set; } = "All Showrooms";
    public DateTime CreatedAt { get; set; }
    public string LoggedDateFormatted => CreatedAt.ToString("MMM dd, yyyy");
    public string? ResolutionNotes { get; set; }
}

public class ComplaintPipelineDto
{
    public int NewCount { get; set; }
    public int InvestigatingCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int EscalatedCount { get; set; }
    public int TotalCount { get; set; }
    public double ResolutionRate { get; set; }
    public List<ComplaintRowViewModel> Rows { get; set; } = new();
}