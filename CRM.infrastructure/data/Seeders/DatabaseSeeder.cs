using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;

namespace CRM.infrastructure.data;

public static class DatabaseSeeder
{
    public static async Task ResetAndSeedDatabaseAsync(Func<TenantCrmDbContext> contextFactory, int companyId = 1)
    {
        await using var db = contextFactory();

        // 1. Drop and recreate database schema cleanly
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var today = new DateTime(2026, 9, 21); // Current operational date

        // -------------------------------------------------------------
        // 2. PARENT TENANT & DATABASE (Resolves FK Constraint)
        // -------------------------------------------------------------
        var company = new Company
        {
            CompanyCode = "ATELIER-01",
            CompanyName = "Atelier Haute Couture",
            IsActive = true,
            CreatedAt = today.AddYears(-1)
        };
        db.Set<Company>().Add(company);
        await db.SaveChangesAsync();

        var companyDb = new CompanyDatabase
        {
            CompanyId = company.CompanyId,
            ServerName = "10.0.2.2,1433",
            DatabaseName = "DB_TenantCRM",
            CredentialKey = "DefaultKey",
            IsActive = true
        };
        db.Set<CompanyDatabase>().Add(companyDb);
        await db.SaveChangesAsync();

        int activeCompanyId = companyDb.CompanyDatabaseId;

        // -------------------------------------------------------------
        // 3. SYSTEM CONFIGURATIONS & POLICIES
        // -------------------------------------------------------------
        db.SystemConfigurations.AddRange(new List<SystemConfiguration>
        {
            new() { ConfigKey = "DefaultLateFeePerDay", ConfigValue = "500.00", Description = "Daily penalty for overdue garment returns", LastModified = today },
            new() { ConfigKey = "StandardDepositPct", ConfigValue = "50.0", Description = "Standard security deposit percentage", LastModified = today },
            new() { ConfigKey = "CleaningBufferDays", ConfigValue = "2", Description = "Required turnaround days for dry cleaning", LastModified = today }
        });

        db.LoyaltyAwards.AddRange(new List<LoyaltyAward>
        {
            new() { TierName = "Standard", MinLifetimeSpend = 0, MinRentalCount = 0, DiscountPercentage = 0.0m, RewardDescription = "Standard boutique membership and priority catalog notifications" },
            new() { TierName = "Gold", MinLifetimeSpend = 8000, MinRentalCount = 2, DiscountPercentage = 5.0m, RewardDescription = "5% off wardrobe leases and complimentary fitting adjustments" },
            new() { TierName = "VIP", MinLifetimeSpend = 15000, MinRentalCount = 3, DiscountPercentage = 10.0m, RewardDescription = "10% off, free minor alterations, and preview access to designer collections" }
        });

        db.Set<RentalTerm>().Add(new RentalTerm
        {
            PolicyTitle = "Atelier Haute Couture Lease Agreement",
            PolicyContent = "All garments must be returned in the garment bag provided. Minor dry cleaning is included. Major stains, tears, or unapproved alterations require assessment from security deposits. Returns past due date incur ₱500/day late fees.",
            VersionNumber = "2.2",
            IsActive = true,
            EffectiveDate = today.AddMonths(-12)
        });
        await db.SaveChangesAsync();

        // -------------------------------------------------------------
        // 4. CUSTOMERS (30 Profiles)
        // -------------------------------------------------------------
        var firstNames = new[] { "Jonalyn", "Margot", "Vivienne", "Celeste", "Joshua", "Janin", "Gwen Giesha", "Aurelia", "Serena", "Camille", "Fleur", "Dominique", "Isabella", "Beatrice", "Katrina", "Danica", "Patricia", "Eleanor", "Samantha", "Lucille", "Genevieve", "Roxanne", "Sophia", "Yvette", "Bianca", "Kassandra", "Natalie", "Marian", "Corazon", "Therese" };
        var lastNames = new[] { "Gelay", "Ellison", "Hartwell", "Moreau", "Ambos", "Lagmay", "Goya", "Fontaine", "Blackwood", "Beaumont", "Delacroix", "Vanier", "Rosario", "Zobel", "Halili", "Reyes", "Tan", "Vance", "Lee", "Mercado", "Castillo", "Villanueva", "Soriano", "Salvador", "Perez", "Aquino", "Mendoza", "Santos", "Alcantara", "Valdez" };
        var districts = new[] { "Lanang, Davao City", "Matina, Davao City", "Bajada, Davao City", "Buhangin, Davao City", "Toril, Davao City", "Ecoland, Davao City", "Tagum City", "Panacan, Davao City", "Obrero, Davao City", "Calinan, Davao City" };

        var customers = new List<Customer>();
        var rng = new Random(42);

        for (int i = 0; i < 30; i++)
        {
            customers.Add(new Customer
            {
                CompanyId = activeCompanyId,
                CustomerCode = $"CUST-{(i + 1):D4}",
                FirstName = firstNames[i % firstNames.Length],
                LastName = lastNames[i % lastNames.Length],
                ContactNumber = $"+63 9{rng.Next(10, 99)} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                EmailAddress = $"{firstNames[i].ToLower().Replace(" ", "")}.{lastNames[i].ToLower()}@example.com",
                Address = districts[i % districts.Length],
                BustSize = rng.Next(31, 39),
                WaistSize = rng.Next(23, 31),
                HipSize = rng.Next(33, 41),
                CreatedAt = today.AddMonths(-rng.Next(2, 7))
            });
        }
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // -------------------------------------------------------------
        // 5. GARMENTS (25 Designer Wardrobe Items)
        // -------------------------------------------------------------
        var garmentTemplates = new (string Code, string Style, string Cat, string Color, string Size, decimal Rate, decimal Dep)[]
        {
            ("GOW-001", "Blush Silk A-Line Gown", "Evening Gown", "Dusty Rose", "M", 4500m, 2500m),
            ("GOW-002", "Champagne Mermaid Silhouette", "Gala Gown", "Champagne Gold", "S", 5200m, 3000m),
            ("BAL-001", "Midnight Velvet Corset Ball Gown", "Ball Gown", "Midnight Blue", "S", 6800m, 4000m),
            ("MID-001", "Emerald Pleated Midi Dress", "Cocktail", "Emerald Green", "M", 3200m, 2000m),
            ("GOW-003", "Scarlet Off-Shoulder Satin Dress", "Evening Gown", "Scarlet Red", "L", 4800m, 2500m),
            ("BAL-002", "Ivory Chantilly Lace Bridal Gown", "Bridal", "Ivory", "M", 9500m, 6000m),
            ("SLP-001", "Noir Cowl-Neck Silk Slip Dress", "Cocktail", "Obsidian Black", "XS", 3000m, 1800m),
            ("GOW-004", "Periwinkle Tulle Illusion Gown", "Prom / Gala", "Periwinkle", "S", 5600m, 3200m),
            ("MOD-001", "Rose Gold Sequin Column Gown", "Black Tie", "Rose Gold", "M", 6200m, 3500m),
            ("JMP-001", "Crepe Tuxedo Tailored Jumpsuit", "Formal Pantsuit", "Ivory White", "L", 3800m, 2200m),
            ("GOW-005", "Bordeaux High-Slit Chiffon Gown", "Evening Gown", "Burgundy", "M", 4600m, 2500m),
            ("BAL-003", "Silver Organza Tiered Ball Gown", "Ball Gown", "Metallic Silver", "S", 7200m, 4500m),
            ("GOW-006", "Sapphire Cape-Sleeve Gown", "Gala Gown", "Royal Sapphire", "L", 5900m, 3500m),
            ("MID-002", "Terracotta Linen Wrap Dress", "Resort Semi-Formal", "Terracotta", "M", 2800m, 1500m),
            ("SLP-002", "Plum Satin Backless Slip", "Cocktail", "Deep Plum", "S", 3400m, 2000m),
            ("GOW-007", "Lilac Floral Organza A-Line", "Spring Gala", "Soft Lilac", "M", 4900m, 2800m),
            ("GOW-008", "Amber Duchess Satin Column", "Evening Gown", "Amber Gold", "L", 5100m, 3000m),
            ("BAL-004", "Onyx Tulle Crystal Ball Gown", "Ball Gown", "Onyx Black", "S", 8200m, 5000m),
            ("MOD-002", "Cobalt High-Neck Halter Gown", "Gala Gown", "Cobalt Blue", "M", 4700m, 2800m),
            ("MID-003", "Champagne Tiered Cocktail Dress", "Cocktail", "Champagne", "S", 3300m, 2000m),
            ("JMP-002", "Obsidian Velvet Shawl-Lapel Jumpsuit", "Formal Pantsuit", "Deep Black", "M", 4200m, 2500m),
            ("GOW-009", "Dusty Lavender Draped Chiffon", "Evening Gown", "Lavender", "L", 4600m, 2600m),
            ("BAL-005", "Peony Pink French Tulle Ball Gown", "Ball Gown", "Peony Pink", "M", 7800m, 4500m),
            ("SLP-003", "Sage Crinkled Charmeuse Slip", "Cocktail", "Sage Green", "XS", 3100m, 1800m),
            ("GOW-010", "Ruby Sweetheart Bustier Trumpet Gown", "Black Tie", "Ruby Red", "S", 6400m, 3800m)
        };

        var garments = new List<Garment>();
        foreach (var t in garmentTemplates)
        {
            garments.Add(new Garment
            {
                CompanyId = activeCompanyId,
                ItemCode = t.Code,
                StyleName = t.Style,
                Category = t.Cat,
                Color = t.Color,
                Size = t.Size,
                BustSize = t.Size == "XS" ? 31 : t.Size == "S" ? 33 : t.Size == "M" ? 35 : 38,
                WaistSize = t.Size == "XS" ? 23 : t.Size == "S" ? 25 : t.Size == "M" ? 27 : 30,
                HipSize = t.Size == "XS" ? 33 : t.Size == "S" ? 35 : t.Size == "M" ? 37 : 40,
                RentalRate = t.Rate,
                SecurityDeposit = t.Dep,
                ReplacementValue = t.Rate * 6.5m,
                Status = "Available",
                CreatedAt = today.AddMonths(-6)
            });
        }
        db.Garments.AddRange(garments);
        await db.SaveChangesAsync();

        // -------------------------------------------------------------
        // 6. PROCEDURAL GENERATION: EXACTLY 200 RENTAL BOOKINGS
        // -------------------------------------------------------------
        // Monthly distribution:
        // Apr: 25 | May: 30 | Jun: 35 | Jul: 40 | Aug: 45 | Sep: 25 = 200 bookings
        var monthQuotas = new (int Year, int Month, int Count)[]
        {
            (2026, 4, 25),
            (2026, 5, 30),
            (2026, 6, 35),
            (2026, 7, 40),
            (2026, 8, 45),
            (2026, 9, 25)
        };

        var paymentMethods = new[] { "Credit Card", "GCash", "Bank Transfer", "Cash", "Debit Card" };
        var bookings = new List<RentalBooking>();

        foreach (var (yr, mo, count) in monthQuotas)
        {
            int daysInMonth = DateTime.DaysInMonth(yr, mo);

            for (int i = 0; i < count; i++)
            {
                var customer = customers[rng.Next(customers.Count)];
                int startDay = (mo == 9)
                    ? rng.Next(1, 22) // September staggered around today (Sep 21)
                    : rng.Next(1, Math.Max(2, daysInMonth - 5));

                var startDate = new DateTime(yr, mo, startDay);
                int duration = rng.Next(3, 6);
                var endDate = startDate.AddDays(duration);

                // Stage determination
                string stage = "Returned";
                if (mo == 9)
                {
                    // Controlled breakdown for current operational month:
                    if (i < 12)
                    {
                        stage = "Returned"; // Settled early September
                    }
                    else if (i < 18)
                    {
                        // Active leases out now, due back within the upcoming 7 days
                        stage = "Active";
                        startDate = today.AddDays(-rng.Next(1, 4));
                        endDate = today.AddDays(rng.Next(1, 7)); // Triggers upcoming returns KPI
                    }
                    else if (i < 21)
                    {
                        stage = "Fitting";
                        startDate = today.AddDays(rng.Next(1, 4));
                        endDate = startDate.AddDays(4);
                    }
                    else if (i < 24)
                    {
                        stage = "Reserved";
                        startDate = today.AddDays(rng.Next(4, 10));
                        endDate = startDate.AddDays(4);
                    }
                    else
                    {
                        // Overdue return: ended before today but still active
                        stage = "Active";
                        startDate = today.AddDays(-7);
                        endDate = today.AddDays(-2);
                    }
                }

                // 15% chance of multi-item rental (2 garments)
                int itemsCount = (rng.Next(100) < 15) ? 2 : 1;
                var selectedGarments = new List<Garment>();
                while (selectedGarments.Count < itemsCount)
                {
                    var g = garments[rng.Next(garments.Count)];
                    if (!selectedGarments.Contains(g)) selectedGarments.Add(g);
                }

                decimal totalFee = selectedGarments.Sum(g => g.RentalRate);
                decimal totalDep = selectedGarments.Sum(g => g.SecurityDeposit);

                var booking = new RentalBooking
                {
                    CompanyId = activeCompanyId,
                    CustomerId = customer.CustomerId,
                    RentalStartDate = startDate,
                    RentalEndDate = endDate,
                    RentalFee = totalFee,
                    SecurityDeposit = totalDep,
                    TotalAmount = totalFee + totalDep,
                    BookingStage = stage,
                    PaymentMethod = paymentMethods[rng.Next(paymentMethods.Length)],
                    AlterationNotes = rng.Next(100) < 30 ? "Minor hem adjustment requested" : null,
                    AgreedToTerms = true,
                    CreatedAt = startDate.AddDays(-rng.Next(3, 10))
                };

                foreach (var garment in selectedGarments)
                {
                    booking.BookingDetails.Add(new BookingDetail
                    {
                        GarmentId = garment.GarmentId,
                        UnitPrice = garment.RentalRate,
                        AlterationNotes = booking.AlterationNotes
                    });
                }

                bookings.Add(booking);
            }
        }

        db.RentalBookings.AddRange(bookings);
        await db.SaveChangesAsync(); // Generates all 200 Booking IDs

        // -------------------------------------------------------------
        // 7. SYNCHRONIZE WARDROBE ASSET STATUSES
        // -------------------------------------------------------------
        // Set garments locked in active leases to "Rented"
        var activeGarmentIds = bookings
            .Where(b => b.BookingStage == "Active")
            .SelectMany(b => b.BookingDetails.Select(d => d.GarmentId))
            .Distinct()
            .ToHashSet();

        var reservedGarmentIds = bookings
            .Where(b => b.BookingStage is "Reserved" or "Fitting")
            .SelectMany(b => b.BookingDetails.Select(d => d.GarmentId))
            .Distinct()
            .ToHashSet();

        foreach (var g in garments)
        {
            if (activeGarmentIds.Contains(g.GarmentId))
            {
                g.Status = "Rented";
            }
            else if (reservedGarmentIds.Contains(g.GarmentId))
            {
                g.Status = "Reserved";
            }
            else
            {
                g.Status = "Available";
            }
        }

        // Set 2 recently returned dresses to "In Cleaning"
        garments[13].Status = "In Cleaning";
        garments[14].Status = "In Cleaning";

        await db.SaveChangesAsync();

        // -------------------------------------------------------------
        // 8. INQUIRIES PIPELINE (30 Leads Across Stages)
        // -------------------------------------------------------------
        var eventTypes = new[] { "Wedding", "Gala", "Prom / Debut", "Black Tie Awards", "Cocktail Party", "Fashion Editorial", "Anniversary" };
        var priorities = new[] { "High", "Medium", "Low" };
        var stages = new[] { "New", "In Review", "Quoted", "Converted", "Closed" };
        var budgetRanges = new[] { "₱3,000–₱5,000", "₱5,000–₱8,000", "₱8,000–₱12,000", "₱12,000+" };

        var inquiries = new List<Inquiry>();
        for (int i = 0; i < 30; i++)
        {
            string status = stages[i % stages.Length];
            var createdDate = today.AddDays(-rng.Next(1, 30));

            inquiries.Add(new Inquiry
            {
                CompanyId = activeCompanyId,
                InquiryCode = $"INQ-{(i + 1):D4}",
                ClientName = $"{firstNames[(i + 5) % firstNames.Length]} {lastNames[(i + 3) % lastNames.Length]}",
                ClientEmail = $"inquiry{(i + 1)}@clientmail.com",
                ClientPhone = $"+63 9{rng.Next(10, 99)} {rng.Next(100, 999)} {rng.Next(1000, 9999)}",
                EventType = eventTypes[rng.Next(eventTypes.Length)],
                EventDate = today.AddDays(rng.Next(5, 45)),
                GarmentRequest = $"Requesting formal wardrobe style: {garmentTemplates[i % garmentTemplates.Length].Style}",
                BudgetRange = budgetRanges[rng.Next(budgetRanges.Length)],
                Priority = priorities[rng.Next(priorities.Length)],
                Status = status,
                InquiryType = (i % 5 == 0) ? "Consultation" : "Rental Request",
                Notes = status == "Converted" ? "Converted into official rental booking." : "Initial intake completed via boutique website.",
                CreatedAt = createdDate
            });
        }

        db.Inquiries.AddRange(inquiries);
        await db.SaveChangesAsync();
    }
}