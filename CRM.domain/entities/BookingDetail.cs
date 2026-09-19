using System.Text.Json.Serialization;

namespace CRM.domain.entities;

public class BookingDetail
{
    public int BookingDetailId { get; set; }
    public int RentalBookingId { get; set; }
    public int GarmentId { get; set; }

    public decimal UnitPrice { get; set; }
    public string? AlterationNotes { get; set; }

    // Navigation properties
    [JsonIgnore]
    public RentalBooking RentalBooking { get; set; } = null!;
    public Garment Garment { get; set; } = null!;
}