using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class EditTenantSubscriptionDialogForm : Form
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly int _companyId;

    private Label lblBoutiqueName = null!;
    private ComboBox cboPackages = null!;
    private Panel pnlPackageCard = null!;
    private Label lblPkgPrice = null!;
    private Label lblPkgBranches = null!;
    private Label lblPkgFeatures = null!;
    private Label lblPkgDescription = null!;

    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpDueDate = null!;
    private CheckBox chkIsActive = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    private List<SubscriptionPackage> _packages = [];
    private Company? _tenant;

    public EditTenantSubscriptionDialogForm(Func<MasterCrmDbContext> masterDbFactory, int companyId)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _companyId = companyId;

        BuildLayout();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void BuildLayout()
    {
        Text = "Manage Tenant Subscription";
        Size = new Size(520, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(28, 20, 28, 20);

        var lblTitle = new Label
        {
            Text = "Modify Subscription Plan",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 16),
            AutoSize = true
        };

        lblBoutiqueName = new Label
        {
            Text = "Loading boutique tenant...",
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(28, 44),
            AutoSize = true
        };
        Controls.AddRange(new Control[] { lblTitle, lblBoutiqueName });

        int y = 78;

        // 1. Subscription Package Dropdown
        var lblPkg = new Label
        {
            Text = "SELECT CRM SUBSCRIPTION PACKAGE *",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, y),
            AutoSize = true
        };

        cboPackages = new ComboBox
        {
            Location = new Point(28, y + 20),
            Width = 448,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10f),
            BackColor = ColorCardBg
        };
        cboPackages.SelectedIndexChanged += (s, e) => UpdatePackageDetailsCard();
        Controls.AddRange(new Control[] { lblPkg, cboPackages });
        y += 62;

        // 2. Package Entitlements Preview Card
        pnlPackageCard = new Panel
        {
            Location = new Point(28, y),
            Size = new Size(448, 130),
            BackColor = ColorCardBg,
            Padding = new Padding(14, 10, 14, 10)
        };
        pnlPackageCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlPackageCard.Width - 1, pnlPackageCard.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };

        lblPkgPrice = new Label
        {
            Text = "₱0.00 / month",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(14, 10),
            AutoSize = true
        };

        lblPkgBranches = new Label
        {
            Text = "Showrooms: 1 location",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(14, 38),
            AutoSize = true
        };

        lblPkgFeatures = new Label
        {
            Text = "Features: Core Leasing, Customer Intake",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorSubtext,
            Location = new Point(14, 60),
            Size = new Size(420, 20)
        };

        lblPkgDescription = new Label
        {
            Text = "Package details...",
            Font = new Font("Segoe UI", 8f),
            ForeColor = ColorMutedLabel,
            Location = new Point(14, 82),
            Size = new Size(420, 38)
        };

        pnlPackageCard.Controls.AddRange(new Control[] { lblPkgPrice, lblPkgBranches, lblPkgFeatures, lblPkgDescription });
        Controls.Add(pnlPackageCard);
        y += 142;

        // 3. Subscription Start Date
        var lblStart = new Label
        {
            Text = "SUBSCRIPTION ENROLLMENT DATE *",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, y),
            AutoSize = true
        };

        dtpStartDate = new DateTimePicker
        {
            Location = new Point(28, y + 20),
            Width = 448,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.AddRange(new Control[] { lblStart, dtpStartDate });
        y += 62;

        // 4. Next Payment Due Date
        var lblDue = new Label
        {
            Text = "NEXT MONTHLY BILLING DUE DATE *",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, y),
            AutoSize = true
        };

        dtpDueDate = new DateTimePicker
        {
            Location = new Point(28, y + 20),
            Width = 448,
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f)
        };
        Controls.AddRange(new Control[] { lblDue, dtpDueDate });
        y += 62;

        // 5. Active Status Checkbox
        chkIsActive = new CheckBox
        {
            Text = "Subscription is active (uncheck to suspend tenant access)",
            Checked = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, y),
            Size = new Size(448, 26),
            Cursor = Cursors.Hand
        };
        Controls.Add(chkIsActive);

        // Action Toolbar
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 36),
            Location = new Point(pnlActions.Width - 235, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = ColorBorder;

        btnSave = new Button
        {
            Text = "Apply Package",
            Size = new Size(130, 36),
            Location = new Point(pnlActions.Width - 130, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveChangesAsync();

        pnlActions.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(pnlActions);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            await using var db = _masterDbFactory();

            _packages = await db.SubscriptionPackages
                .AsNoTracking()
                .OrderBy(p => p.MonthlyFee)
                .ToListAsync();

            cboPackages.DataSource = _packages;
            cboPackages.DisplayMember = "PackageName";
            cboPackages.ValueMember = "SubscriptionPackageId";

            _tenant = await db.Companies
                .AsNoTracking()
                .Include(c => c.SubscriptionPackage)
                .FirstOrDefaultAsync(c => c.CompanyId == _companyId);

            if (_tenant == null)
            {
                MessageBox.Show("Tenant record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            lblBoutiqueName.Text = $"{_tenant.CompanyName} ({_tenant.CompanyCode})";
            cboPackages.SelectedValue = _tenant.SubscriptionPackageId;
            dtpStartDate.Value = _tenant.SubscriptionStartDate;
            dtpDueDate.Value = _tenant.NextBillingDueDate;
            chkIsActive.Checked = _tenant.IsActive;

            UpdatePackageDetailsCard();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load subscription: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdatePackageDetailsCard()
    {
        if (cboPackages.SelectedItem is not SubscriptionPackage pkg) return;

        lblPkgPrice.Text = $"₱{pkg.MonthlyFee:N2} / month";
        lblPkgBranches.Text = pkg.HasMultiBranch
            ? $"Showrooms: Multi-Location (Up to {pkg.MaxBranches} branches)"
            : "Showrooms: Single Boutique Location (1 showroom)";

        var features = new List<string>();
        if (pkg.HasLoyalty) features.Add("Client Loyalty Tiers");
        if (pkg.HasAnalytics) features.Add("Executive BI Analytics");
        if (pkg.HasMultiBranch) features.Add("Showroom Switcher");

        lblPkgFeatures.Text = features.Count > 0
            ? $"Included: {string.Join(" • ", features)}"
            : "Included: Core Rentals, Client Intake, Fitting Notes";

        lblPkgDescription.Text = string.IsNullOrWhiteSpace(pkg.Description)
            ? "Standard CRM boutique tier."
            : pkg.Description;
    }

    private async Task SaveChangesAsync()
    {
        if (cboPackages.SelectedItem is not SubscriptionPackage selectedPkg) return;

        try
        {
            btnSave.Enabled = false;
            await using var db = _masterDbFactory();

            var tenant = await db.Companies.FindAsync(_companyId);
            if (tenant == null) return;

            // Warn if downgrading a tenant from a multi-branch package to a single-branch package
            if (!selectedPkg.HasMultiBranch && tenant.SubscriptionPackageId != selectedPkg.SubscriptionPackageId)
            {
                var prompt = MessageBox.Show(
                    $"Warning: Switching to '{selectedPkg.PackageName}' restricts this tenant to {selectedPkg.MaxBranches} showroom location and disables the showroom selector.\n\n" +
                    "Do you want to proceed with this tier change?",
                    "Confirm Package Downgrade",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (prompt != DialogResult.Yes)
                {
                    btnSave.Enabled = true;
                    return;
                }
            }

            tenant.SubscriptionPackageId = selectedPkg.SubscriptionPackageId;
            tenant.SubscriptionStartDate = dtpStartDate.Value.Date;
            tenant.NextBillingDueDate = dtpDueDate.Value.Date;
            tenant.IsActive = chkIsActive.Checked;
            tenant.DeactivatedAt = chkIsActive.Checked ? null : DateTime.UtcNow;

            var dbs = await db.CompanyDatabases.Where(d => d.CompanyId == _companyId).ToListAsync();
            foreach (var d in dbs) d.IsActive = tenant.IsActive;

            await db.SaveChangesAsync();

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to update subscription: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
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