using System;
using System.Collections.Generic;
using System.Linq;
using CRM.domain.entities;

namespace CRM.winforms.Models;

public class BookingDraftModel
{
    public Customer? SelectedCustomer { get; set; }
    public List<Garment> SelectedGarments { get; set; } = new();

    public DateTime RentalStartDate { get; set; } = DateTime.Today;
    public DateTime RentalEndDate { get; set; } = DateTime.Today.AddDays(7);

    public decimal FittingBust { get; set; }
    public decimal FittingWaist { get; set; }
    public decimal FittingHip { get; set; }
    public string? AlterationNotes { get; set; }

    public string SelectedPaymentMethod { get; set; } = "Credit Card – Visa ••••4832";

    // Financial computations
    public int RentalDurationDays => Math.Max(1, (RentalEndDate.Date - RentalStartDate.Date).Days);

    public decimal TotalRentalFee => SelectedGarments.Sum(g => g.RentalRate);
    public decimal TotalSecurityDeposit => SelectedGarments.Sum(g => g.SecurityDeposit);
    public decimal TotalDue => TotalRentalFee + TotalSecurityDeposit;

    public bool AgreedToTerms { get; set; }
}