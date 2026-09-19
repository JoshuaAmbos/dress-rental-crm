using System.Text.Json.Serialization;

namespace CRM.domain.entities;

public class BookingDetail
{
    public int BookingDetailId { get; set; }
    public int RentalBookingId { get; set; }
    public int RentalItemId { get; set; }
    public string? AlterationNotes { get; set; }

    // Navigation properties
    [JsonIgnore]
    public RentalBooking RentalBooking { get; set; } = null!;

    public Garment RentalItem { get; set; } = null!;
}