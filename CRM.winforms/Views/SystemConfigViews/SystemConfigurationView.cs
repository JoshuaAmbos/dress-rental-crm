using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class SystemConfigurationView : UserControl
{
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

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorViewBg = Color.FromArgb(250, 245, 245);
    private static readonly Color ColorCardBg = Color.White;

    public SystemConfigurationView(Func<TenantCrmDbContext> contextFactory)
    {
        _controller = new SystemConfigController(contextFactory);

        BuildUI();
        _ = LoadConfigurationsAsync();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header Section
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "System Configuration & Operational Parameters",
            UseMnemonic = false, // Fixes the underscore glitch
            Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblLastAudit = new Label
        {
            Text = "Super Admin Exclusive Control • Baseline Business Rules",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorSubtext,
            Location = new Point(2, 28),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblLastAudit });

        // Scrollable Body Container
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 20, 8)
        };

        const int cardWidth = 860;

        // --- Card 1: Financial & Billing Parameters ---
        int card1Y = 38;
        var cardFinance = CreateCardContainer("FINANCIAL & BILLING PARAMETERS", 0, 8, cardWidth, 275);
        txtLateFee = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Daily Overdue Penalty (₱/day):", "Assessed per day overdue during post-return garment inspection (UC-03).");
        txtDepositPct = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Standard Deposit Percentage (%):", "Default percentage of garment rental fee collected as refundable security deposit (UC-02).");
        txtCleaningFee = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Standard Dry-Cleaning Surcharge (₱):", "Fee deducted from security deposits for minor stain treatments in incident tickets.");
        txtTaxRate = AddSettingRow(cardFinance, ref card1Y, cardWidth, "Sales Tax / VAT (%):", "Standard percentage added to rental invoices during transaction settlement.");

        // --- Card 2: Turnaround & Operational Policies ---
        int card2Y = 38;
        var cardLogistics = CreateCardContainer("OPERATIONAL POLICIES & TURNAROUND", 0, cardFinance.Bottom + 16, cardWidth, 165);
        txtCleaningBuffer = AddSettingRow(cardLogistics, ref card2Y, cardWidth, "Dry-Cleaning Buffer (Days):", "Calendar days reserved after return before a garment becomes available in catalog scheduling.");
        txtMaxRentals = AddSettingRow(cardLogistics, ref card2Y, cardWidth, "Max Active Leases Per Client:", "Maximum concurrent active gown leases allowed without administrative override.");

        // Bottom Action Toolbar
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Color.Transparent
        };

        btnReset = new Button
        {
            Text = "Restore Defaults",
            Size = new Size(140, 36),
            Location = new Point(0, 8),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ColorEspresso,
            BackColor = Color.FromArgb(245, 240, 240),
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnReset.FlatAppearance.BorderSize = 0;
        btnReset.Click += async (s, e) => await ResetDefaultsAsync();

        btnSave = new Button
        {
            Text = "Save Configuration",
            Size = new Size(160, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlActions.Width - 160, 8),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveChangesAsync();

        pnlActions.Controls.AddRange(new Control[] { btnReset, btnSave });

        pnlBody.Controls.AddRange(new Control[] { cardFinance, cardLogistics });

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
            ForeColor = ColorDustyRose,
            Location = new Point(24, 14),
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
            ForeColor = ColorEspresso,
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

        var txt = new TextBox
        {
            Location = new Point(cardWidth - 240, currentY + 6),
            Width = 216,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            TextAlign = HorizontalAlignment.Right
        };

        parentCard.Controls.AddRange(new Control[] { lblTitle, lblDesc, txt });
        currentY += 56;
        return txt;
    }

    public async Task LoadConfigurationsAsync()
    {
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