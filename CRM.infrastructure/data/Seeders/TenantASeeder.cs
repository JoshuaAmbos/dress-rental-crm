using CRM.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data.Seeders;

public class TenantASeeder : ITenantSeeder
{
    public int CompanyId => 2;
    public string CompanyCode => "MAISON-02";

    public async Task SeedAsync(TenantCrmDbContext db)
    {
        var today = new DateTime(2026, 9, 21);

        // 1. Company Record (Let SQL Server auto-generate CompanyId)
        var company = await db.Set<Company>().FirstOrDefaultAsync(c => c.CompanyCode == CompanyCode);
        if (company == null)
        {
            company = new Company
            {
                CompanyCode = CompanyCode,
                CompanyName = "Maison Étoile Bridal",
                IsActive = true,
                CreatedAt = today.AddYears(-1)
            };
            db.Set<Company>().Add(company);
            await db.SaveChangesAsync();
        }

        // 2. Ensure CompanyDatabase exists linked to company.CompanyId
        var companyDb = await db.Set<CompanyDatabase>().FirstOrDefaultAsync(cd => cd.CompanyId == company.CompanyId);
        if (companyDb == null)
        {
            companyDb = new CompanyDatabase
            {
                CompanyId = company.CompanyId,
                ServerName = "10.0.2.2,1433",
                DatabaseName = "DB_TenantCRM",
                CredentialKey = "DefaultKey",
                IsActive = true
            };
            db.Set<CompanyDatabase>().Add(companyDb);
            await db.SaveChangesAsync();
        }

        // 3. Target Company ID is company.CompanyId (NOT companyDb.CompanyDatabaseId)
        int targetCompanyId = company.CompanyId;

        // 4. Single Branch under Tenant 2
        var singleBranch = new Branch
        {
            CompanyId = targetCompanyId,
            BranchCode = "ME-MAIN",
            BranchName = "Flagship Atelier",
            City = "Makati City",
            Address = "Paseo de Roxas, Makati",
            ContactPhone = "+63 2 8111 2233",
            IsActive = true
        };
        db.Branches.Add(singleBranch);
        await db.SaveChangesAsync();

        // 5. Garments under Tenant 2
        var garments = new List<Garment>
        {
            new() { CompanyId = targetCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-101", StyleName = "Celestial Silk Organza Gown", Category = "Bridal", Color = "Off-White", Size = "S", RentalRate = 12000m, SecurityDeposit = 6000m, ReplacementValue = 75000m, Status = "Available" },
            new() { CompanyId = targetCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-102", StyleName = "Royal Chantilly Veil Ensemble", Category = "Bridal", Color = "Pure White", Size = "M", RentalRate = 15000m, SecurityDeposit = 7500m, ReplacementValue = 90000m, Status = "Available" },
            new() { CompanyId = targetCompanyId, BranchId = singleBranch.BranchId, ItemCode = "BRD-103", StyleName = "Minimalist Mikado Ballgown", Category = "Bridal", Color = "Ivory", Size = "L", RentalRate = 9800m, SecurityDeposit = 5000m, ReplacementValue = 60000m, Status = "Available" }
        };
        db.Garments.AddRange(garments);

        // 6. Customers under Tenant 2
        db.Customers.AddRange(new List<Customer>
        {
            new() { CompanyId = targetCompanyId, CustomerCode = "CUST-A001", FirstName = "Adrianna", LastName = "Vanderbilt", ContactNumber = "+63 917 111 2233", EmailAddress = "a.vanderbilt@clientmail.com", Address = "Dasmariñas Village, Makati" },
            new() { CompanyId = targetCompanyId, CustomerCode = "CUST-A002", FirstName = "Clara", LastName = "Montenegro", ContactNumber = "+63 917 444 5566", EmailAddress = "c.montenegro@clientmail.com", Address = "Forbes Park, Makati" }
        });

        await db.SaveChangesAsync();
    }
}