using CRM.api.DTOs;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class BranchService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public BranchService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<List<BranchDto>> GetBranchesAsync(int companyId)
    {
        await using var db = _contextFactory();
        return await db.Branches
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .Select(b => new BranchDto
            {
                BranchId = b.BranchId,
                BranchCode = b.BranchCode,
                BranchName = b.BranchName,
                City = b.City,
                TotalGarments = b.Garments.Count,
                ActiveRentals = b.RentalBookings.Count(r => r.BookingStage == "Active")
            })
            .ToListAsync();
    }
}