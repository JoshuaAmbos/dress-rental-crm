using CRM.domain.entities;

namespace CRM.winforms.Models;

public class GarmentPickerRowViewModel
{
    public Garment GarmentEntity { get; set; } = null!;
    public int GarmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SizeLabel { get; set; } = string.Empty;
    public decimal RentalRate { get; set; }
    public decimal SecurityDeposit { get; set; }
}