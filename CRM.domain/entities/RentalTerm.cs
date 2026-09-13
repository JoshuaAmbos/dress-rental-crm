namespace CRM.domain.entities;

public class RentalTerm
{
    public int RentalTermId { get; set; }
    public string PolicyTitle { get; set; } = string.Empty;
    public string PolicyContent { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
}