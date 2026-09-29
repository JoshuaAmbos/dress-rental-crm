using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Controllers;

public class TermsController
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public TermsController(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<RentalTerm?> GetActivePolicyAsync()
    {
        await using var db = _contextFactory();
        return await db.RentalTerms
            .AsNoTracking()
            .OrderByDescending(t => t.EffectiveDate)
            .FirstOrDefaultAsync(t => t.IsActive);
    }

    public async Task<List<RentalTerm>> GetAllVersionsAsync()
    {
        await using var db = _contextFactory();
        return await db.RentalTerms
            .AsNoTracking()
            .OrderByDescending(t => t.EffectiveDate)
            .ThenByDescending(t => t.RentalTermId)
            .ToListAsync();
    }

    public async Task SetActiveVersionAsync(int termId)
    {
        await using var db = _contextFactory();
        var allTerms = await db.RentalTerms.ToListAsync();

        foreach (var term in allTerms)
        {
            term.IsActive = (term.RentalTermId == termId);
        }

        await db.SaveChangesAsync();
    }

    public async Task UpdateExistingPolicyAsync(int termId, string title, string version, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Policy Title is required.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Policy Content cannot be empty.");

        await using var db = _contextFactory();
        var existing = await db.RentalTerms.FirstOrDefaultAsync(t => t.RentalTermId == termId);
        if (existing == null)
            throw new InvalidOperationException("Selected agreement does not exist.");

        existing.PolicyTitle = title.Trim();
        existing.VersionNumber = string.IsNullOrWhiteSpace(version) ? existing.VersionNumber : version.Trim();
        existing.PolicyContent = content.Trim();

        await db.SaveChangesAsync();
    }

    public async Task<RentalTerm> PublishNewVersionAsync(string title, string version, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Policy Title is required.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Policy Content cannot be empty.");

        await using var db = _contextFactory();

        var currentActives = await db.RentalTerms.Where(t => t.IsActive).ToListAsync();
        foreach (var t in currentActives)
        {
            t.IsActive = false;
        }

        var newTerm = new RentalTerm
        {
            PolicyTitle = title.Trim(),
            VersionNumber = string.IsNullOrWhiteSpace(version) ? "1.0" : version.Trim(),
            PolicyContent = content.Trim(),
            EffectiveDate = DateTime.UtcNow,
            IsActive = true
        };

        db.RentalTerms.Add(newTerm);
        await db.SaveChangesAsync();
        return newTerm;
    }
}