using System.Text.Json.Serialization;

namespace CRM.domain.entities;

public class ServiceIncident
{
    public int ServiceIncidentId { get; set; }
    public int RentalBookingId { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public decimal FeeCharged { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public DateTime LoggedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    [JsonIgnore]
    public RentalBooking RentalBooking { get; set; } = null!;
}