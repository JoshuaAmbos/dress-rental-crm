using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class SystemConfigurationView : UserControl
{
    private const string DefaultConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly SystemConfigController _controller;

    // Financial Inputs
    private TextBox txtLateFee = null!;
    private TextBox txtDepositPct = null!;
    private TextBox txtCleaningFee = null!;
    private TextBox txtTaxRate = null!;

    // Turnaround Inputs
    private TextBox txtCleaningBuffer = null!;
    private TextBox txtMaxRentals = null!;

    private Label lblLastAudit = null!;
    private Button btnSave = null!;
    private Button btnReset = null!;

    // Parameterless constructor for WinForms Designer support
    public SystemConfigurationView() : this(() =>
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(DefaultConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    })
    {
    }

    public SystemConfigurationView(Func<TenantCrmDbContext> contextFactory)
    {
        _controller = new SystemConfigController(contextFactory ?? throw new ArgumentNullException(nameof(contextFactory)));

        BuildUI();
        _ = LoadConfigurationsAsync();
    }

    private void BuildUI()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header Section
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "System Configuration & Operational Parameters",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblLastAudit = new Label
        {
            Text = "Super Admin Exclusive Control • Baseline Business Rules",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblLastAudit });

        // 2. Bottom Action Toolbar
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        btnReset = new Button
        {
            Text = "Restore Defaults",
            Size = new Size(150, 38),
            Location = new Point(0, 10),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ColorNavInactiveText,
            BackColor = ColorCardBg,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnReset.FlatAppearance.BorderSize = 1;
        btnReset.FlatAppearance.BorderColor = ColorBorder;
        btnReset.MouseEnter += (s, e) =>
        {
            btnReset.BackColor = ColorActivePill;
            btnReset.ForeColor = ColorPrimary;
        };
        btnReset.MouseLeave += (s, e) =>
        {
            btnReset.BackColor = ColorCardBg;
            btnReset.ForeColor = ColorNavInactiveText;
        };
        btnReset.Click += async (s, e) => await ResetDefaultsAsync();

        btnSave = new Button
        {
            Text = "Save Configuration",
            Size = new Size(170, 38),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlActions.Width - 170, 10),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ColorAccent,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = ColorAccentHover;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = ColorAccent;
        btnSave.Click += async (s, e) => await SaveChangesAsync();

        pnlActions.Resize += (s, e) =>
        {
            btnSave.Location = new Point(Math.Max(160, pnlActions.ClientSize.Width - btnSave.Width), 10);
        };

        pnlActions.Controls.AddRange(new Control[] { btnReset, btnSave });

        // 3. Scrollable Body Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 20, 8)
        };

        const int cardWidth = 860;

        // --- Card 1: Financial & Billing Parameters ---
        int card1Y = 42;
        var cardFinance = CreateCardContainer("FINANCIAL & BILLING PARAMETERS", 0, 8, cardWidth, 275);
        txtLateFee = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Daily Overdue Penalty (₱/day):", "Assessed per day overdue during post-return garment inspection (UC-03).");
        txtDepositPct = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Standard Deposit Percentage (%):", "Default percentage of garment rental fee collected as refundable security deposit (UC-02).");
        txtCleaningFee = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Standard Dry-Cleaning Surcharge (₱):", "Fee deducted from security deposits for minor stain treatments in incident tickets.");
        txtTaxRate = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Sales Tax / VAT (%):", "Standard percentage added to rental invoices during transaction settlement.");

        // --- Card 2: Turnaround & Operational Policies ---
        int card2Y = 42;
        var cardLogistics = CreateCardContainer("OPERATIONAL POLICIES & TURNAROUND", 0, cardFinance.Bottom + 16, cardWidth, 168);
        txtCleaningBuffer = AddSettingRow(cardLogistics, ref card2Y, cardWidth, "Dry-Cleaning Buffer (Days):", "Calendar days reserved after return before a garment becomes available in catalog scheduling.");
        txtMaxRentals = AddSettingRow(cardLogistics, ref card2Y, cardWidth, "Max Active Leases Per Client:", "Maximum concurrent active gown leases allowed without administrative override.");

        pnlBody.Controls.AddRange(new Control[] { cardFinance, cardLogistics });

        // Assembly
        Controls.Add(pnlBody);
        Controls.Add(pnlActions);
        Controls.Add(pnlHeader);
    }

    private static Panel CreateCardContainer(string title, int x, int y, int width, int height)
    {
        var card = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = ColorCardBg,
            Padding = new Padding(24, 16, 24, 16)
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.25f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

        var lblCat = new Label
        {
            Text = title,
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(24, 16),
            AutoSize = true
        };

        card.Controls.Add(lblCat);
        return card;
    }

    private static TextBox AddSettingRow(Panel parentCard, ref int currentY, int cardWidth, string title, string description)
    {
        var lblTitle = new Label
        {
            Text = title,
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(24, currentY),
            AutoSize = true
        };

        var lblDesc = new Label
        {
            Text = description,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Location = new Point(24, currentY + 22),
            Size = new Size(540, 24),
            AutoEllipsis = true
        };

        // Enclosed border panel for modern text box styling
        var pnlTextBorder = new Panel
        {
            Location = new Point(cardWidth - 240, currentY + 6),
            Size = new Size(216, 32),
            BackColor = ColorCardBg,
            Padding = new Padding(8, 6, 8, 4)
        };
        pnlTextBorder.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlTextBorder.Width - 1, pnlTextBorder.Height - 1), 4);
            e.Graphics.DrawPath(pen, path);
        };

        var txt = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
            ForeColor = ColorBrandDark,
            BackColor = ColorCardBg,
            TextAlign = HorizontalAlignment.Right
        };

        pnlTextBorder.Controls.Add(txt);
        parentCard.Controls.AddRange(new Control[] { lblTitle, lblDesc, pnlTextBorder });
        currentY += 56;
        return txt;
    }

    public async Task LoadConfigurationsAsync()
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        try
        {
            var configs = await _controller.GetAllConfigurationsAsync();
            var dict = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue, StringComparer.OrdinalIgnoreCase);

            txtLateFee.Text = dict.GetValueOrDefault("DefaultLateFeePerDay", "500.00");
            txtDepositPct.Text = dict.GetValueOrDefault("StandardDepositPct", "50.0");
            txtTaxRate.Text = dict.GetValueOrDefault("TaxRatePercentage", "12.0");

            txtCleaningFee.Text = dict.TryGetValue("CleaningFeeRate", out var cfr)
                ? cfr
                : dict.GetValueOrDefault("StandardCleaningFee", "350.00");

            txtCleaningBuffer.Text = dict.GetValueOrDefault("CleaningBufferDays", "2");
            txtMaxRentals.Text = dict.GetValueOrDefault("MaxActiveRentalsPerClient", "3");

            var latest = configs.OrderByDescending(c => c.LastModified).FirstOrDefault();
            if (latest != null)
            {
                lblLastAudit.Text = $"Last Modified: {latest.LastModified:MMM dd, yyyy hh:mm tt} (UTC) • Super Admin Access Only";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load configurations: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveChangesAsync()
    {
        if (!decimal.TryParse(txtLateFee.Text, out var lateFee) || lateFee < 0 ||
            !decimal.TryParse(txtDepositPct.Text, out var depositPct) || depositPct < 0 || depositPct > 100 ||
            !decimal.TryParse(txtTaxRate.Text, out var taxRate) || taxRate < 0 ||
            !int.TryParse(txtCleaningBuffer.Text, out var bufferDays) || bufferDays < 0 ||
            !decimal.TryParse(txtCleaningFee.Text, out var cleanFee) || cleanFee < 0 ||
            !int.TryParse(txtMaxRentals.Text, out var maxRentals) || maxRentals < 1)
        {
            MessageBox.Show("Please ensure all parameters contain valid non-negative numerical values.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnSave.Enabled = false;
            btnSave.Text = "Saving...";

            var dict = new Dictionary<string, string>
            {
                { "DefaultLateFeePerDay", lateFee.ToString("F2") },
                { "StandardDepositPct", depositPct.ToString("F1") },
                { "TaxRatePercentage", taxRate.ToString("F1") },
                { "CleaningBufferDays", bufferDays.ToString() },
                { "CleaningFeeRate", cleanFee.ToString("F2") },
                { "MaxActiveRentalsPerClient", maxRentals.ToString() }
            };

            await _controller.SaveConfigurationsAsync(dict);
            MessageBox.Show("System configurations updated successfully.", "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadConfigurationsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save configurations: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSave.Enabled = true;
            btnSave.Text = "Save Configuration";
        }
    }

    private async Task ResetDefaultsAsync()
    {
        var confirm = MessageBox.Show(
            "Reset all boutique parameters back to baseline factory defaults?",
            "Confirm Reset",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            await _controller.ResetToDefaultsAsync();
            await LoadConfigurationsAsync();
        }
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}