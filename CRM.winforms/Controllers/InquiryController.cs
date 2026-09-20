using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Models;

namespace CRM.winforms.Controllers;

public class InquiryController
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public InquiryController(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<InquiryPipelineDto> LoadPipelineAsync(int companyId, string statusFilter, string search)
    {
        await using var db = _contextFactory();

        var query = db.Inquiries
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId);

        // Stage counters
        int newCount = await query.CountAsync(i => i.Status == "New");
        int inReviewCount = await query.CountAsync(i => i.Status == "In Review");
        int quotedCount = await query.CountAsync(i => i.Status == "Quoted");
        int convertedCount = await query.CountAsync(i => i.Status == "Converted");
        int closedCount = await query.CountAsync(i => i.Status == "Closed");
        int totalCount = await query.CountAsync();

        // Apply tab status filter
        if (!string.IsNullOrEmpty(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Status == statusFilter);
        }

        // Apply search keyword filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(i =>
                i.InquiryCode.ToLower().Contains(term) ||
                i.ClientName.ToLower().Contains(term) ||
                (i.ClientEmail != null && i.ClientEmail.ToLower().Contains(term)) ||
                (i.EventType != null && i.EventType.ToLower().Contains(term)) ||
                (i.GarmentRequest != null && i.GarmentRequest.ToLower().Contains(term)));
        }

        // Project directly into InquiryRowViewModel
        var rows = await query
            .OrderByDescending(i => i.InquiryId)
            .Select(i => new InquiryRowViewModel
            {
                InquiryId = i.InquiryId,
                InquiryCode = i.InquiryCode,
                ClientName = i.ClientName,
                ClientEmail = i.ClientEmail ?? string.Empty,
                EventType = i.EventType ?? "—",
                EventDate = i.EventDate,
                GarmentRequest = i.GarmentRequest ?? "—",
                BudgetRange = i.BudgetRange ?? "—",
                Priority = i.Priority ?? "Medium",
                Status = i.Status ?? "New"
            })
            .ToListAsync();

        return new InquiryPipelineDto
        {
            NewCount = newCount,
            InReviewCount = inReviewCount,
            QuotedCount = quotedCount,
            ConvertedCount = convertedCount,
            ClosedCount = closedCount,
            TotalCount = totalCount,
            Rows = rows
        };
    }

    public async Task<Inquiry?> GetInquiryAsync(int inquiryId)
    {
        await using var db = _contextFactory();
        return await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
    }

    public async Task SaveInquiryAsync(Inquiry model)
    {
        await using var db = _contextFactory();

        if (model.InquiryId == 0)
        {
            if (string.IsNullOrEmpty(model.InquiryCode))
            {
                int count = await db.Inquiries.CountAsync(i => i.CompanyId == model.CompanyId);
                model.InquiryCode = $"INQ-{(count + 1):D4}";
            }

            db.Inquiries.Add(model);
        }
        else
        {
            var existing = await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == model.InquiryId);
            if (existing != null)
            {
                existing.ClientName = model.ClientName;
                existing.ClientPhone = model.ClientPhone;
                existing.ClientEmail = model.ClientEmail;
                existing.EventType = model.EventType;
                existing.EventDate = model.EventDate;
                existing.GarmentRequest = model.GarmentRequest;
                existing.BudgetRange = model.BudgetRange;
                existing.Priority = model.Priority;
                existing.Status = model.Status;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task MarkConvertedAsync(int inquiryId)
    {
        await using var db = _contextFactory();
        var inquiry = await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
        if (inquiry != null)
        {
            inquiry.Status = "Converted";
            await db.SaveChangesAsync();
        }
    }
}