using CRM.api.DTOs;
using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api.Services;

public class ComplaintService
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly EmailNotificationService _emailService;

    public ComplaintService(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _emailService = new EmailNotificationService(_contextFactory);
    }

    public async Task<ComplaintPipelineDto> GetPipelineAsync(int companyId, int? branchId = null, string statusFilter = "All", string search = "")
    {
        await using var db = _contextFactory();

        var query = db.Complaints
            .AsNoTracking()
            .Include(c => c.Branch)
            .Where(c => c.CompanyId == companyId);

        if (branchId.HasValue)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        int totalCount = await query.CountAsync();
        int newCount = await query.CountAsync(c => c.Status == "New");
        int investigatingCount = await query.CountAsync(c => c.Status == "Under Investigation");
        int inProgressCount = await query.CountAsync(c => c.Status == "In Progress");
        int resolvedCount = await query.CountAsync(c => c.Status == "Resolved");
        int escalatedCount = await query.CountAsync(c => c.Status == "Escalated");

        double resolutionRate = totalCount > 0 ? Math.Round((double)resolvedCount / totalCount * 100.0, 1) : 100.0;

        if (!string.IsNullOrEmpty(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(c =>
                c.ComplaintCode.ToLower().Contains(term) ||
                c.ClientName.ToLower().Contains(term) ||
                (c.ClientPhone != null && c.ClientPhone.Contains(term)) ||
                (c.Category != null && c.Category.ToLower().Contains(term)) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ComplaintRowViewModel
            {
                ComplaintId = c.ComplaintId,
                ComplaintCode = c.ComplaintCode,
                ClientName = c.ClientName,
                ClientPhone = c.ClientPhone ?? "—",
                Category = c.Category,
                Severity = c.Severity,
                Status = c.Status,
                Description = c.Description,
                CompensationAmount = c.CompensationAmount,
                BranchName = c.Branch != null ? c.Branch.BranchName : "All Showrooms",
                CreatedAt = c.CreatedAt,
                ResolutionNotes = c.ResolutionNotes
            })
            .ToListAsync();

        return new ComplaintPipelineDto
        {
            TotalCount = totalCount,
            NewCount = newCount,
            InvestigatingCount = investigatingCount,
            InProgressCount = inProgressCount,
            ResolvedCount = resolvedCount,
            EscalatedCount = escalatedCount,
            ResolutionRate = resolutionRate,
            Rows = rows
        };
    }

    public async Task<Complaint?> GetComplaintAsync(int complaintId)
    {
        await using var db = _contextFactory();
        return await db.Complaints
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
    }

    public async Task SaveComplaintAsync(Complaint model)
    {
        await using var db = _contextFactory();
        bool isNew = model.ComplaintId == 0;
        bool wasJustResolved = false;

        if (isNew)
        {
            if (string.IsNullOrEmpty(model.ComplaintCode))
            {
                int count = await db.Complaints.CountAsync(c => c.CompanyId == model.CompanyId);
                model.ComplaintCode = $"CMP-{(count + 1):D4}";
            }

            model.CreatedAt = DateTime.UtcNow;
            db.Complaints.Add(model);
        }
        else
        {
            var existing = await db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == model.ComplaintId);
            if (existing != null)
            {
                wasJustResolved = model.Status == "Resolved" && existing.Status != "Resolved";

                existing.ClientName = model.ClientName;
                existing.ClientPhone = model.ClientPhone;
                existing.ClientEmail = model.ClientEmail;
                existing.BranchId = model.BranchId;
                existing.Category = model.Category;
                existing.Severity = model.Severity;
                existing.Status = model.Status;
                existing.Description = model.Description;
                existing.ResolutionNotes = model.ResolutionNotes;
                existing.CompensationAmount = model.CompensationAmount;

                if (model.Status == "Resolved" && !existing.ResolvedAt.HasValue)
                {
                    existing.ResolvedAt = DateTime.UtcNow;
                }
            }
        }

        await db.SaveChangesAsync();

        // Dispatch Complaint Notifications
        if (isNew && !string.IsNullOrWhiteSpace(model.ClientEmail))
        {
            _ = _emailService.SendNotificationAsync(
                model.CompanyId,
                model.BranchId,
                null,
                model.ClientEmail,
                model.ClientName,
                $"Incident Ticket Logged ({model.ComplaintCode})",
                "Complaints",
                $"We received your report regarding <strong>{model.Category}</strong>. " +
                $"Our showroom management has initiated a review under status: <em>{model.Status}</em>.");
        }
        else if (wasJustResolved && !string.IsNullOrWhiteSpace(model.ClientEmail))
        {
            _ = _emailService.SendNotificationAsync(
                model.CompanyId,
                model.BranchId,
                null,
                model.ClientEmail,
                model.ClientName,
                $"Incident Resolved ({model.ComplaintCode})",
                "Complaints",
                $"Your complaint has been marked as <strong>Resolved</strong>.<br/>" +
                $"Resolution: <em>{model.ResolutionNotes ?? "Corrective action completed."}</em>" +
                (model.CompensationAmount > 0 ? $"<br/>A credit/refund of <strong>₱{model.CompensationAmount:N2}</strong> was authorized." : ""));
        }
    }

    public async Task ResolveComplaintAsync(int complaintId, string notes, decimal compensation)
    {
        await using var db = _contextFactory();
        var existing = await db.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
        if (existing != null)
        {
            existing.Status = "Resolved";
            existing.ResolutionNotes = notes;
            existing.CompensationAmount = compensation;
            existing.ResolvedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(existing.ClientEmail))
            {
                _ = _emailService.SendNotificationAsync(
                    existing.CompanyId,
                    existing.BranchId,
                    null,
                    existing.ClientEmail,
                    existing.ClientName,
                    $"Incident Resolved ({existing.ComplaintCode})",
                    "Complaints",
                    $"Your complaint has been marked as <strong>Resolved</strong>.<br/>" +
                    $"Resolution: <em>{notes}</em>" +
                    (compensation > 0 ? $"<br/>A credit/refund of <strong>₱{compensation:N2}</strong> was authorized." : ""));
            }
        }
    }
}