using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Services;

public class InquiryService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;

    public InquiryService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<InquiryPipelineDto> GetPipelineAsync(int companyId, string statusFilter = "All", string search = "")
    {
        await using var db = _contextFactory();

        var query = db.Inquiries.AsNoTracking().Where(i => i.CompanyId == companyId);

        var allInquiries = await query.ToListAsync();

        int newCount = allInquiries.Count(i => i.Status == "New");
        int inReviewCount = allInquiries.Count(i => i.Status == "In Review");
        int quotedCount = allInquiries.Count(i => i.Status == "Quoted");
        int convertedCount = allInquiries.Count(i => i.Status == "Converted");
        int closedCount = allInquiries.Count(i => i.Status == "Closed");

        var filtered = allInquiries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            filtered = filtered.Where(i => string.Equals(i.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            filtered = filtered.Where(i =>
                i.InquiryCode.ToLower().Contains(term) ||
                i.ClientName.ToLower().Contains(term) ||
                i.ClientEmail.ToLower().Contains(term) ||
                i.EventType.ToLower().Contains(term) ||
                i.GarmentRequest.ToLower().Contains(term));
        }

        var rows = filtered
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InquiryRowViewModel
            {
                InquiryId = i.InquiryId,
                InquiryCode = i.InquiryCode,
                ClientName = i.ClientName,
                ClientEmail = i.ClientEmail,
                EventType = i.EventType,
                EventDate = i.EventDate,
                GarmentRequest = i.GarmentRequest,
                BudgetRange = i.BudgetRange,
                Priority = i.Priority,
                Status = i.Status
            })
            .ToList();

        return new InquiryPipelineDto
        {
            NewCount = newCount,
            InReviewCount = inReviewCount,
            QuotedCount = quotedCount,
            ConvertedCount = convertedCount,
            ClosedCount = closedCount,
            TotalCount = allInquiries.Count,
            Rows = rows
        };
    }

    public async Task<Inquiry?> GetByIdAsync(int id)
    {
        await using var db = _contextFactory();
        return await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == id);
    }

    public async Task SaveOrUpdateAsync(Inquiry inquiry)
    {
        await using var db = _contextFactory();

        if (inquiry.InquiryId == 0)
        {
            int count = await db.Inquiries.CountAsync(i => i.CompanyId == inquiry.CompanyId);
            inquiry.InquiryCode = $"INQ-{(count + 1):D4}";
            db.Inquiries.Add(inquiry);
        }
        else
        {
            db.Inquiries.Update(inquiry);
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = _contextFactory();
        var item = await db.Inquiries.FindAsync(id);
        if (item != null)
        {
            db.Inquiries.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}