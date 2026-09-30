using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Services;

public class SuperadminReportPrinter
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly Func<TenantCrmDbContext> _tenantDbFactory;

    public SuperadminReportPrinter(
        Func<MasterCrmDbContext> masterDbFactory,
        Func<TenantCrmDbContext> tenantDbFactory)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _tenantDbFactory = tenantDbFactory ?? throw new ArgumentNullException(nameof(tenantDbFactory));
    }

    public async Task OpenReportPreviewAsync(string categoryTitle, string requestedBy, IWin32Window parent)
    {
        // 1. Gather all master and tenant metadata
        await using var masterDb = _masterDbFactory();
        var tenants = await masterDb.Companies
            .AsNoTracking()
            .Include(c => c.SubscriptionPackage)
            .Include(c => c.CompanyDatabases)
            .OrderBy(c => c.CompanyId)
            .ToListAsync();

        var databases = await masterDb.CompanyDatabases
            .AsNoTracking()
            .Include(d => d.Company)
            .OrderBy(d => d.CompanyId)
            .ToListAsync();

        var rawUsers = await (from u in masterDb.Users
                              join ur in masterDb.UserRoles on u.Id equals ur.UserId into userRoles
                              from ur in userRoles.DefaultIfEmpty()
                              join r in masterDb.Roles on ur.RoleId equals r.Id into roles
                              from r in roles.DefaultIfEmpty()
                              join uc in masterDb.UserClaims.Where(c => c.ClaimType == "CompanyId") on u.Id equals uc.UserId into companyClaims
                              from uc in companyClaims.DefaultIfEmpty()
                              join un in masterDb.UserClaims.Where(c => c.ClaimType == "CompanyName") on u.Id equals un.UserId into nameClaims
                              from un in nameClaims.DefaultIfEmpty()
                              join ub in masterDb.UserClaims.Where(c => c.ClaimType == "BranchName") on u.Id equals ub.UserId into branchClaims
                              from ub in branchClaims.DefaultIfEmpty()
                              select new
                              {
                                  u.UserName,
                                  u.Email,
                                  Role = r != null ? r.Name : "Staff",
                                  CompanyName = un != null ? un.ClaimValue : "Atelier Haute Couture",
                                  Branch = ub != null ? ub.ClaimValue : "All Showrooms"
                              }).ToListAsync();

        // 2. Set up native PrintDocument
        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"Atelier_Audit_Report_{DateTime.Now:yyyyMMdd}";
        printDoc.DefaultPageSettings.Margins = new Margins(45, 45, 45, 45);

        printDoc.PrintPage += (s, e) =>
        {
            var g = e.Graphics!;
            int left = e.MarginBounds.Left;
            int right = e.MarginBounds.Right;
            int y = e.MarginBounds.Top;

            using var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var headerFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            using var boldFont = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);

            using var brushDark = new SolidBrush(ColorBrandDark);
            using var brushAccent = new SolidBrush(ColorAccent);
            using var brushSub = new SolidBrush(ColorSubtext);
            using var brushCard = new SolidBrush(ColorCardBg);
            using var penLine = new Pen(ColorBorder, 1f);

            // Document Header
            g.DrawString("ATELIER CRM — MULTI-TENANT ARCHITECTURE", subFont, brushAccent, left, y);
            y += 18;
            g.DrawString($"System Audit Report: {categoryTitle}", titleFont, brushDark, left, y);
            y += 32;

            g.DrawString($"Auditor: {requestedBy}   |   Generated: {DateTime.Now:MMM dd, yyyy HH:mm}   |   Confidential", textFont, brushSub, left, y);
            y += 18;
            g.DrawLine(penLine, left, y, right, y);
            y += 14;

            // Summary Stats Strip
            int cardW = (e.MarginBounds.Width - 24) / 4;
            DrawSummaryBox(g, left, y, cardW, 44, "TENANTS", $"{tenants.Count} Registered", $"{tenants.Count(t => t.IsActive)} Active");
            DrawSummaryBox(g, left + cardW + 8, y, cardW, 44, "DATABASES", $"{databases.Count(d => d.IsActive)} Online", "Isolated SQL DBs");
            DrawSummaryBox(g, left + (cardW * 2) + 16, y, cardW, 44, "IDENTITIES", $"{rawUsers.Count} Accounts", "Across all tenants");
            decimal mrr = tenants.Where(t => t.IsActive).Sum(t => t.SubscriptionPackage?.MonthlyFee ?? 0);
            DrawSummaryBox(g, left + (cardW * 3) + 24, y, cardW, 44, "MONTHLY MRR", $"₱{mrr:N0}", "Active Subscriptions");
            y += 56;

            // Table 1: Boutique Tenants
            if (categoryTitle.Contains("Tenants") || categoryTitle.Contains("Master"))
            {
                g.DrawString("1. BOUTIQUE TENANTS & LICENSING", headerFont, brushDark, left, y);
                y += 18;

                g.FillRectangle(brushCard, left, y, e.MarginBounds.Width, 22);
                g.DrawRectangle(penLine, left, y, e.MarginBounds.Width, 22);
                g.DrawString("CODE", boldFont, brushSub, left + 8, y + 4);
                g.DrawString("BOUTIQUE TENANT", boldFont, brushSub, left + 90, y + 4);
                g.DrawString("PACKAGE", boldFont, brushSub, left + 300, y + 4);
                g.DrawString("FEE/MO", boldFont, brushSub, left + 460, y + 4);
                g.DrawString("STATUS", boldFont, brushSub, left + 550, y + 4);
                y += 24;

                foreach (var t in tenants)
                {
                    g.DrawString(t.CompanyCode, boldFont, brushDark, left + 8, y + 3);
                    g.DrawString(t.CompanyName, textFont, brushDark, left + 90, y + 3);
                    g.DrawString(t.SubscriptionPackage?.PackageName ?? "None", textFont, brushDark, left + 300, y + 3);
                    g.DrawString($"₱{(t.SubscriptionPackage?.MonthlyFee ?? 0):N0}", textFont, brushDark, left + 460, y + 3);
                    g.DrawString(t.IsActive ? "Active" : "Suspended", boldFont, t.IsActive ? Brushes.Green : Brushes.Firebrick, left + 550, y + 3);
                    y += 18;
                    g.DrawLine(penLine, left, y, right, y);
                    y += 4;
                }
                y += 16;
            }

            // Table 2: Database Routing
            if (categoryTitle.Contains("Databases") || categoryTitle.Contains("Master"))
            {
                g.DrawString("2. DATABASE INFRASTRUCTURE & ROUTING MAP", headerFont, brushDark, left, y);
                y += 18;

                g.FillRectangle(brushCard, left, y, e.MarginBounds.Width, 22);
                g.DrawRectangle(penLine, left, y, e.MarginBounds.Width, 22);
                g.DrawString("TENANT", boldFont, brushSub, left + 8, y + 4);
                g.DrawString("DATABASE NAME", boldFont, brushSub, left + 180, y + 4);
                g.DrawString("HOST / SERVER", boldFont, brushSub, left + 380, y + 4);
                g.DrawString("STATE", boldFont, brushSub, left + 550, y + 4);
                y += 24;

                foreach (var d in databases)
                {
                    g.DrawString(d.Company?.CompanyName ?? $"Company #{d.CompanyId}", textFont, brushDark, left + 8, y + 3);
                    g.DrawString(d.DatabaseName, boldFont, brushAccent, left + 180, y + 3);
                    g.DrawString(d.ServerName, textFont, brushDark, left + 380, y + 3);
                    g.DrawString(d.IsActive ? "Online" : "Offline", boldFont, d.IsActive ? Brushes.Green : Brushes.Firebrick, left + 550, y + 3);
                    y += 18;
                    g.DrawLine(penLine, left, y, right, y);
                    y += 4;
                }
                y += 16;
            }

            // Table 3: User Accounts
            if (categoryTitle.Contains("Users") || categoryTitle.Contains("Master"))
            {
                g.DrawString("3. USER ACCOUNTS & SECURITY DIRECTORY", headerFont, brushDark, left, y);
                y += 18;

                g.FillRectangle(brushCard, left, y, e.MarginBounds.Width, 22);
                g.DrawRectangle(penLine, left, y, e.MarginBounds.Width, 22);
                g.DrawString("USERNAME", boldFont, brushSub, left + 8, y + 4);
                g.DrawString("EMAIL ADDRESS", boldFont, brushSub, left + 140, y + 4);
                g.DrawString("ROLE", boldFont, brushSub, left + 340, y + 4);
                g.DrawString("SHOWROOM BRANCH", boldFont, brushSub, left + 450, y + 4);
                y += 24;

                foreach (var u in rawUsers.Take(12))
                {
                    g.DrawString(u.UserName ?? "", boldFont, brushDark, left + 8, y + 3);
                    g.DrawString(u.Email ?? "", textFont, brushDark, left + 140, y + 3);
                    g.DrawString(u.Role, textFont, brushDark, left + 340, y + 3);
                    g.DrawString(u.Branch, textFont, brushDark, left + 450, y + 3);
                    y += 18;
                    g.DrawLine(penLine, left, y, right, y);
                    y += 4;
                }
            }

            // Document Footer
            int footerY = e.MarginBounds.Bottom - 12;
            g.DrawLine(penLine, left, footerY, right, footerY);
            g.DrawString("Atelier Dress Rental CRM — Architecture Control Plane", textFont, brushSub, left, footerY + 4);
            g.DrawString("Page 1 of 1", boldFont, brushDark, right - 65, footerY + 4);

            e.HasMorePages = false;
        };

        // 3. Open native WinForms Print Preview Dialog
        using var previewDlg = new PrintPreviewDialog
        {
            Document = printDoc,
            WindowState = FormWindowState.Maximized,
            Text = $"Print Preview — {categoryTitle}"
        };

        previewDlg.ShowDialog(parent);
    }

    private static void DrawSummaryBox(Graphics g, int x, int y, int w, int h, string title, string val, string sub)
    {
        using var pen = new Pen(ColorBorder, 1f);
        using var brushBg = new SolidBrush(ColorCardBg);
        using var fontTitle = new Font("Segoe UI Semibold", 7f, FontStyle.Bold);
        using var fontVal = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 7f, FontStyle.Regular);

        g.FillRectangle(brushBg, x, y, w, h);
        g.DrawRectangle(pen, x, y, w, h);

        g.DrawString(title, fontTitle, new SolidBrush(ColorSubtext), x + 6, y + 4);
        g.DrawString(val, fontVal, new SolidBrush(ColorBrandDark), x + 6, y + 16);
        g.DrawString(sub, fontSub, new SolidBrush(ColorAccent), x + 6, y + 30);
    }
}