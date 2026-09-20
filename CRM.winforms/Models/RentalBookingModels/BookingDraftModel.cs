using CRM.domain.entities;

namespace CRM.winforms.models;

public class BookingDraftModel
{
    public Customer? SelectedCustomer { get; set; }
    public List<Garment> SelectedGarments { get; set; } = [];
    public DateTime RentalStartDate { get; set; } = DateTime.Today;
    public DateTime RentalEndDate { get; set; } = DateTime.Today.AddDays(3);

    public decimal FittingBust { get; set; }
    public decimal FittingWaist { get; set; }   
    public decimal FittingHip { get; set; }
    public string? AlterationNotes { get; set; }

    public decimal TotalRentalFee => SelectedGarments.Sum(g => g.RentalRate);
    public decimal TotalSecurityDeposit => SelectedGarments.Sum(g => g.SecurityDeposit);

    public bool AgreedToTerms { get; set; }

}