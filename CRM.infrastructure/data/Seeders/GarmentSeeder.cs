using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;

namespace CRM.infrastructure.data;

public static class GarmentSeeder
{
    public static async Task SeedGarmentsAsync(TenantCrmDbContext context, int tenantCompanyId = 1)
    {
        int companyId = tenantCompanyId <= 0 ? 1 : tenantCompanyId;

        // 1. Ensure Company exists in dbo.Company to satisfy the foreign key constraint
        var companyCount = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(1) AS Value FROM dbo.Company WHERE CompanyId = {0}", companyId)
            .FirstOrDefaultAsync();

        if (companyCount == 0)
        {
            // Insert the base company record if missing
            await context.Database.ExecuteSqlRawAsync(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.Company WHERE CompanyId = {0})
                  BEGIN
                      SET IDENTITY_INSERT dbo.Company ON;
                      INSERT INTO dbo.Company (CompanyId, CompanyCode, CompanyName, IsActive, CreatedAt)
                      VALUES ({0}, 'ATELIER01', 'Atelier Dress Rentals', 1, GETUTCDATE());
                      SET IDENTITY_INSERT dbo.Company OFF;
                  END", companyId);
        }

        // 2. Prevent duplicate garment entries
        if (await context.Garments.AnyAsync(g => g.CompanyId == companyId))
        {
            return;
        }

        // 3. Boutique Wardrobe Catalog Seed Data
        var garments = new List<Garment>
        {
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "GOW-001",
                StyleName = "Blush Silk A-Line Gown",
                Category = "Evening Gown",
                Color = "Dusty Rose",
                Size = "M",
                BustSize = 34.0m,
                WaistSize = 26.5m,
                HipSize = 36.0m,
                RentalRate = 4500.00m,
                SecurityDeposit = 2500.00m,
                ReplacementValue = 18000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "GOW-002",
                StyleName = "Champagne Mermaid Silhouette",
                Category = "Evening Gown",
                Color = "Champagne",
                Size = "S",
                BustSize = 32.0m,
                WaistSize = 24.5m,
                HipSize = 34.5m,
                RentalRate = 5200.00m,
                SecurityDeposit = 3000.00m,
                ReplacementValue = 22000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "FIL-001",
                StyleName = "Modern Embroidered Filipiniana",
                Category = "Filipiniana",
                Color = "Alabaster White",
                Size = "L",
                BustSize = 36.5m,
                WaistSize = 29.0m,
                HipSize = 39.0m,
                RentalRate = 6000.00m,
                SecurityDeposit = 3500.00m,
                ReplacementValue = 25000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "COK-001",
                StyleName = "Emerald Pleated Midi Dress",
                Category = "Cocktail Dress",
                Color = "Emerald Green",
                Size = "M",
                BustSize = 34.5m,
                WaistSize = 27.0m,
                HipSize = 37.0m,
                RentalRate = 3200.00m,
                SecurityDeposit = 2000.00m,
                ReplacementValue = 14000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "BAL-001",
                StyleName = "Midnight Velvet Corset Ball Gown",
                Category = "Ball Gown",
                Color = "Midnight Blue",
                Size = "S",
                BustSize = 33.0m,
                WaistSize = 25.0m,
                HipSize = 35.0m,
                RentalRate = 6800.00m,
                SecurityDeposit = 4000.00m,
                ReplacementValue = 30000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "SUIT-001",
                StyleName = "Tailored Charcoal Tuxedo Suit",
                Category = "Suit / Tuxedo",
                Color = "Charcoal",
                Size = "L",
                BustSize = 40.0m,
                WaistSize = 32.0m,
                HipSize = 40.0m,
                RentalRate = 3800.00m,
                SecurityDeposit = 2500.00m,
                ReplacementValue = 16000.00m,
                Status = "Available",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Garment
            {
                CompanyId = companyId,
                ItemCode = "GOW-003",
                StyleName = "Lavender Organza Off-Shoulder Gown",
                Category = "Evening Gown",
                Color = "Lavender",
                Size = "XS",
                BustSize = 31.0m,
                WaistSize = 23.5m,
                HipSize = 33.5m,
                RentalRate = 4800.00m,
                SecurityDeposit = 2500.00m,
                ReplacementValue = 20000.00m,
                Status = "In Cleaning",
                ImagePath = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Garments.AddRange(garments);
        await context.SaveChangesAsync();
    }
}