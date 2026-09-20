namespace CRM.winforms.Models;

public class InquiryRowViewModel
{
    public int InquiryId { get; set; }
    public string InquiryCode { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime? EventDate { get; set; }
    public string EventDateFormatted => EventDate?.ToString("MMM dd, yyyy") ?? "Flexible";
    public string GarmentRequest { get; set; } = string.Empty;
    public string BudgetRange { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "New";
}

public class InquiryPipelineDto
{
    public int NewCount { get; set; }
    public int InReviewCount { get; set; }
    public int QuotedCount { get; set; }
    public int ConvertedCount { get; set; }
    public int ClosedCount { get; set; }
    public int TotalCount { get; set; }
    public double ConversionRate => TotalCount > 0 ? Math.Round((double)ConvertedCount / TotalCount * 100.0, 1) : 0;
    public List<InquiryRowViewModel> Rows { get; set; } = [];
}