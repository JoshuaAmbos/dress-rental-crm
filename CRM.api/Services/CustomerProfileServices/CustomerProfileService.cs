using CRM.api.DTOs;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class CustomerProfileService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public CustomerProfileService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    // Backwards-compatible overload for wizard steps that don't pass branchId
    public Task<List<CustomerRowViewModel>> GetCustomersAsync(int companyId, bool showArchived, string search = "")
        => GetCustomersAsync(companyId, null, showArchived, search);

    // Multi-branch filtered query
    public async Task<List<CustomerRowViewModel>> GetCustomersAsync(int companyId, int? branchId, bool showArchived, string search = "")
    {
        await using var db = _contextFactory();

        var query = db.Customers
            .AsNoTracking()
            .Include(c => c.Branch)
            .Where(c => c.CompanyId == companyId && c.IsActive == !showArchived);

        // If a specific showroom is selected, filter strictly to that branch
        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerCode.ToLower().Contains(term) ||
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                (c.ContactNumber != null && c.ContactNumber.Contains(term)) ||
                (c.EmailAddress != null && c.EmailAddress.ToLower().Contains(term)) ||
                (c.Address != null && c.Address.ToLower().Contains(term)));
        }

        var customers = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();

        return customers.Select(c =>
        {
            string fullName = $"{c.FirstName} {c.MiddleName} {c.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = !string.IsNullOrWhiteSpace(c.FirstName) ? c.FirstName : $"Client ({c.CustomerCode})";
            }

            return new CustomerRowViewModel
            {
                CustomerEntity = c,
                CustomerId = c.CustomerId,
                Code = c.CustomerCode,
                Name = fullName,
                Phone = string.IsNullOrWhiteSpace(c.ContactNumber) ? "—" : c.ContactNumber,
                Email = string.IsNullOrWhiteSpace(c.EmailAddress) ? "—" : c.EmailAddress,
                Address = string.IsNullOrWhiteSpace(c.Address) ? "—" : c.Address,
                BustSize = c.BustSize > 0 ? $"{c.BustSize:0.#} in" : "—",
                WaistSize = c.WaistSize > 0 ? $"{c.WaistSize:0.#} in" : "—",
                HipSize = c.HipSize > 0 ? $"{c.HipSize:0.#} in" : "—",
                IsActive = c.IsActive
            };
        }).ToList();
    }

    public async Task ArchiveCustomerAsync(int customerId)
    {
        await using var db = _contextFactory();
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (entity != null)
        {
            entity.IsActive = false;
            await db.SaveChangesAsync();
        }
    }

    public async Task RestoreCustomerAsync(int customerId)
    {
        await using var db = _contextFactory();
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (entity != null)
        {
            entity.IsActive = true;
            await db.SaveChangesAsync();
        }
    }
}