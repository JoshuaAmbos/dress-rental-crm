using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Controllers;

public class SystemConfigController
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public SystemConfigController(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<List<SystemConfiguration>> GetAllConfigurationsAsync()
    {
        await using var db = _contextFactory();
        return await db.SystemConfigurations
            .AsNoTracking()
            .OrderBy(c => c.ConfigKey)
            .ToListAsync();
    }

    public async Task SaveConfigurationsAsync(Dictionary<string, string> updatedConfigs)
    {
        await using var db = _contextFactory();
        var keys = updatedConfigs.Keys.ToList();
        var existingEntities = await db.SystemConfigurations.Where(c => keys.Contains(c.ConfigKey)).ToListAsync();

        foreach (var entity in existingEntities)
        {
            if (updatedConfigs.TryGetValue(entity.ConfigKey, out var newVal))
            {
                entity.ConfigValue = newVal.Trim();
                entity.LastModified = DateTime.UtcNow;
            }
        }

        // Add any missing keys dynamically
        var existingKeySet = existingEntities.Select(e => e.ConfigKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in updatedConfigs)
        {
            if (!existingKeySet.Contains(kvp.Key))
            {
                db.SystemConfigurations.Add(new SystemConfiguration
                {
                    ConfigKey = kvp.Key,
                    ConfigValue = kvp.Value.Trim(),
                    LastModified = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task ResetToDefaultsAsync()
    {
        var defaults = new Dictionary<string, string>
        {
            { "DefaultLateFeePerDay", "500.00" },
            { "StandardDepositPct", "50.0" },
            { "CleaningBufferDays", "2" },
            { "CleaningFeeRate", "350.00" },
            { "TaxRatePercentage", "12.0" },
            { "MaxActiveRentalsPerClient", "3" }
        };

        await SaveConfigurationsAsync(defaults);
    }
}