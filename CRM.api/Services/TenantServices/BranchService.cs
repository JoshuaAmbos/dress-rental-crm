using CRM.domain.entities;
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

    public async Task<List<Branch>> GetBranchesAsync(int companyId, string searchTerm = "", bool includeInactive = true)
    {
        await using var db = _contextFactory();
        var query = db.Branches
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(b =>
                b.BranchCode.ToLower().Contains(search) ||
                b.BranchName.ToLower().Contains(search) ||
                b.City.ToLower().Contains(search) ||
                b.Address.ToLower().Contains(search) ||
                b.ContactPhone.Contains(search));
        }

        return await query
            .OrderByDescending(b => b.IsActive)
            .ThenBy(b => b.BranchName)
            .ToListAsync();
    }

    public async Task CreateBranchAsync(Branch branch)
    {
        if (string.IsNullOrWhiteSpace(branch.BranchCode))
            throw new ArgumentException("Branch code is required.");

        if (string.IsNullOrWhiteSpace(branch.BranchName))
            throw new ArgumentException("Branch name is required.");

        await using var db = _contextFactory();
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
    }

    public async Task UpdateBranchAsync(Branch updated)
    {
        await using var db = _contextFactory();
        var existing = await db.Branches.FindAsync(updated.BranchId);
        if (existing == null) return;

        existing.BranchCode = updated.BranchCode.Trim();
        existing.BranchName = updated.BranchName.Trim();
        existing.City = updated.City.Trim();
        existing.Address = updated.Address.Trim();
        existing.ContactPhone = updated.ContactPhone.Trim();
        existing.IsActive = updated.IsActive;

        await db.SaveChangesAsync();
    }

    public async Task ToggleBranchStatusAsync(int branchId)
    {
        await using var db = _contextFactory();
        var branch = await db.Branches.FindAsync(branchId);
        if (branch == null) return;

        branch.IsActive = !branch.IsActive;
        await db.SaveChangesAsync();
    }
}