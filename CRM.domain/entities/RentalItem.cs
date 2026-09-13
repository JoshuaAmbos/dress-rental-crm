namespace CRM.domain.entities;

public class RentalItem
{
    public int RentalItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string StyleName { get; set; } = string.Empty;
    public decimal RentalRate { get; set; }
    public decimal ReplacementValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}