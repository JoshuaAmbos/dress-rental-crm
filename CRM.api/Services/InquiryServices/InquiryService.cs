using CRM.api.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class InquiryService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly EmailNotificationService _emailService;

    public InquiryService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _emailService = new EmailNotificationService(_contextFactory);
    }

    public async Task<InquiryPipelineDto> GetPipelineAsync(int companyId, int? branchId = null, string statusFilter = "All", string search = "")
    {
        await using var db = _contextFactory();

        var query = db.Inquiries
            .AsNoTracking()
            .Include(i => i.Branch)
            .Where(i => i.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(i => i.BranchId == branchId.Value);
        }

        int totalCount = await query.CountAsync();
        int newCount = await query.CountAsync(i => i.Status == "New");
        int inReviewCount = await query.CountAsync(i => i.Status == "In Review");
        int quotedCount = await query.CountAsync(i => i.Status == "Quoted");
        int convertedCount = await query.CountAsync(i => i.Status == "Converted");
        int closedCount = await query.CountAsync(i => i.Status == "Closed");

        double conversionRate = totalCount > 0 ? Math.Round((double)convertedCount / totalCount * 100.0, 1) : 0.0;

        if (!string.IsNullOrEmpty(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(i =>
                i.InquiryCode.ToLower().Contains(term) ||
                i.ClientName.ToLower().Contains(term) ||
                (i.ClientPhone != null && i.ClientPhone.Contains(term)) ||
                (i.EventType != null && i.EventType.ToLower().Contains(term)) ||
                (i.GarmentRequest != null && i.GarmentRequest.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var rows = list.Select(i => new InquiryRowViewModel
        {
            InquiryId = i.InquiryId,
            InquiryCode = i.InquiryCode,
            ClientName = i.ClientName,
            ClientEmail = i.ClientEmail,
            ClientPhone = i.ClientPhone,
            EventType = i.EventType,
            EventDate = i.EventDate,
            GarmentRequest = i.GarmentRequest,
            BudgetRange = i.BudgetRange,
            Priority = i.Priority,
            Status = i.Status,
            BranchName = i.Branch != null ? i.Branch.BranchName : "All Showrooms",
            CreatedAt = i.CreatedAt
        }).ToList();

        return new InquiryPipelineDto
        {
            TotalCount = totalCount,
            NewCount = newCount,
            InReviewCount = inReviewCount,
            QuotedCount = quotedCount,
            ConvertedCount = convertedCount,
            ClosedCount = closedCount,
            Rows = rows
        };
    }

    public async Task<Inquiry?> GetInquiryAsync(int inquiryId)
    {
        await using var db = _contextFactory();
        return await db.Inquiries
            .Include(i => i.Branch)
            .FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
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

            model.CreatedAt = DateTime.UtcNow;
            db.Inquiries.Add(model);
        }
        else
        {
            var existing = await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == model.InquiryId);
            if (existing != null)
            {
                existing.ClientName = model.ClientName;
                existing.ClientEmail = model.ClientEmail;
                existing.ClientPhone = model.ClientPhone;
                existing.BranchId = model.BranchId;
                existing.EventType = model.EventType;
                existing.EventDate = model.EventDate;
                existing.GarmentRequest = model.GarmentRequest;
                existing.BudgetRange = model.BudgetRange;
                existing.Priority = model.Priority;
                existing.Status = model.Status;
                existing.InquiryType = model.InquiryType;
                existing.Notes = model.Notes;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task MarkConvertedAsync(int inquiryId)
    {
        await using var db = _contextFactory();
        var existing = await db.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
        if (existing != null)
        {
            existing.Status = "Converted";
            await db.SaveChangesAsync();

            // Notify client of successful conversion
            if (!string.IsNullOrWhiteSpace(existing.ClientEmail))
            {
                _ = _emailService.SendNotificationAsync(
                    existing.CompanyId,
                    existing.BranchId,
                    null,
                    existing.ClientEmail,
                    existing.ClientName,
                    $"Inquiry Converted to Booking ({existing.InquiryCode})",
                    "Inquiries",
                    $"Your inquiry for <strong>{existing.EventType}</strong> has been confirmed and transferred to our fitting team. Our concierge will be in touch shortly.");
            }
        }
    }
}