using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.infrastructure.services
{

    // fetch only codes matching the prefix for this boutique
    // then parse out the numeric parts
    // and increment and pad width leading zeroes
    // Example: 1 -> CUST-0001

    public class CustomerCodeGenerator
    {
        private readonly Func<TenantCrmDbContext> _dbFactory;

        public CustomerCodeGenerator(Func<TenantCrmDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<string> GenerateNextCodeAsync(int companyId, string prefix = "CUST")
        {
            await using var db = _dbFactory();

            var existingCodes = await db.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.CustomerCode.StartsWith(prefix + "-"))
                .Select(c => c.CustomerCode)
                .ToListAsync();

            int maxNumber = 0;

            foreach (var code in existingCodes)
            {
                var parts = code.Split('-');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedNumber))
                {
                    if (parsedNumber > maxNumber)
                    {
                        maxNumber = parsedNumber;
                    }
                }
            }

            int nextNumber = maxNumber + 1;
            return $"{prefix}-{nextNumber:D4}";
        }

    }
}
