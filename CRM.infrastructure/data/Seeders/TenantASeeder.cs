using CRM.domain.entities;

namespace CRM.infrastructure.data.Seeders;

public class TenantASeeder : ITenantSeeder
{
    public int CompanyId => 2;
    public string CompanyCode => "MAISON-02";

    public async Task SeedAsync(TenantCrmDbContext db)
    {
        var today = new DateTime(2026, 9, 21);

        var company = new Company
        {
            CompanyCode = CompanyCode,
            CompanyName = "Maison Étoile Bridal",
            IsActive = true,
            CreatedAt = today.AddYears(-1)
        };
        db.Set<Company>().Add(company);
        await db.SaveChangesAsync();

        int activeCompanyId = company.CompanyId;

        var singleBranch = new Branch
        {
            CompanyId = activeCompanyId,
            BranchCode = "ME-MAIN",
            BranchName = "Flagship Atelier",
            City = "Makati City",
            Address = "Paseo de Roxas, Makati",
            ContactPhone = "+63 2 8111 2233",
            IsActive = true
        };
        db.Branches.Add(singleBranch);
        await db.SaveChangesAsync();

        var garments = new List<Garment>
        {
            new() { CompanyId = activeCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-101", StyleName = "Celestial Silk Organza Gown", Category = "Bridal", Color = "Off-White", Size = "S", RentalRate = 12000m, SecurityDeposit = 6000m, ReplacementValue = 75000m, Status = "Available" },
            new() { CompanyId = activeCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-102", StyleName = "Royal Chantilly Veil Ensemble", Category = "Bridal", Color = "Pure White", Size = "M", RentalRate = 15000m, SecurityDeposit = 7500m, ReplacementValue = 90000m, Status = "Available" },
            new() { CompanyId = activeCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-103", StyleName = "Minimalist Mikado Ballgown", Category = "Bridal", Color = "Ivory", Size = "L", RentalRate = 9800m, SecurityDeposit = 5000m, ReplacementValue = 60000m, Status = "Available" }
        };
        db.Garments.AddRange(garments);

        db.Customers.AddRange(new List<Customer>
        {
            new() { CompanyId = activeCompanyId, CustomerCode = "CUST-A001", FirstName = "Adrianna", LastName = "Vanderbilt", ContactNumber = "+63 917 111 2233", EmailAddress = "a.vanderbilt@clientmail.com", Address = "Dasmariñas Village, Makati" },
            new() { CompanyId = activeCompanyId, CustomerCode = "CUST-A002", FirstName = "Clara", LastName = "Montenegro", ContactNumber = "+63 917 444 5566", EmailAddress = "c.montenegro@clientmail.com", Address = "Forbes Park, Makati" }
        });

        await db.SaveChangesAsync();
    }
}