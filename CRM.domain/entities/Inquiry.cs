namespace CRM.domain.entities
{
    public class Inquiry
    {
        public int InquiryId { get; set; }
        public int CompanyId { get; set; }
        public string InquiryCode { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public string GarmentRequest { get; set; } = string.Empty;
        public string BudgetRange { get; set; } = "$200–$400";

        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public string Status { get; set; } = "New"; // New, In Review, Quoted, Converted, Closed
        public string? InquiryType { get; set; } = "Rental Request"; // Rental Request, Consultation, Complaint
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
