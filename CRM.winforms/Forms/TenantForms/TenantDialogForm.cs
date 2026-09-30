using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class TenantDialogForm : Form
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly TenantProvisioningService _provisioningService;

    private TextBox txtCompanyCode = null!;
    private TextBox txtCompanyName = null!;
    private ComboBox cboPackages = null!;
    private Label lblPackageDetails = null!;
    private TextBox txtBranchName = null!;
    private TextBox txtCity = null!;
    private Button btnProvision = null!;
    private Button btnCancel = null!;
    private Label lblStatus = null!;

    private List<SubscriptionPackage> _packages = [];

    public TenantDialogForm(
        Func<MasterCrmDbContext> masterDbFactory,
        TenantProvisioningService provisioningService)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));

        BuildLayout();
        Load += async (s, e) => await LoadPackagesAsync();
    }

    private void BuildLayout()
    {
        Text = "Provision New Boutique Tenant";
        Size = new Size(500, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(28, 20, 28, 20);

        var lblTitle = new Label
        {
            Text = "New Boutique Tenant",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Creates an isolated SQL database, seeds default showroom, and binds CRM package.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(28, 44),
            Size = new Size(428, 36)
        };
        Controls.AddRange(new Control[] { lblTitle, lblSub });

        int y = 88;

        // 1. Company Code
        txtCompanyCode = CreateField("COMPANY CODE (e.g., CEBU, DAVAO) *", ref y, "CEBU");

        // 2. Company Name
        txtCompanyName = CreateField("BOUTIQUE / COMPANY NAME *", ref y, "Cebu Bridal & Haute Couture");

        // 3. Subscription Package Selection
        var lblPkg = new Label
        {
            Text = "CRM SUBSCRIPTION PACKAGE *",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, y),
            AutoSize = true
        };

        cboPackages = new ComboBox
        {
            Location = new Point(28, y + 20),
            Width = 428,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = ColorCardBg
        };
        cboPackages.SelectedIndexChanged += (s, e) => UpdatePackageDetailsLabel();

        lblPackageDetails = new Label
        {
            Text = "Loading packages...",
            Font = new Font("Segoe UI", 8.25f),
            ForeColor = ColorAccent,
            Location = new Point(28, y + 54),
            Size = new Size(428, 18)
        };

        Controls.AddRange(new Control[] { lblPkg, cboPackages, lblPackageDetails });
        y += 78;

        // 4. Initial Branch Name
        txtBranchName = CreateField("INITIAL SHOWROOM BRANCH NAME *", ref y, "Main Flagship Studio");

        // 5. Initial Branch City
        txtCity = CreateField("CITY / LOCATION *", ref y, "Cebu City");

        // Status Label
        lblStatus = new Label
        {
            Text = "",
            ForeColor = ColorAccent,
            Font = new Font("Segoe UI", 9f),
            Location = new Point(28, y),
            Size = new Size(428, 22),
            TextAlign = ContentAlignment.MiddleLeft
        };
        Controls.Add(lblStatus);

        // Action Toolbar
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 36),
            Location = new Point(pnlActions.Width - 265, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = ColorBorder;

        btnProvision = new Button
        {
            Text = "Provision Database",
            Size = new Size(160, 36),
            Location = new Point(pnlActions.Width - 160, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnProvision.FlatAppearance.BorderSize = 0;
        btnProvision.Click += async (s, e) => await ExecuteProvisioningAsync();

        pnlActions.Controls.AddRange(new Control[] { btnCancel, btnProvision });
        Controls.Add(pnlActions);

        AcceptButton = btnProvision;
        CancelButton = btnCancel;
    }

    private TextBox CreateField(string label, ref int yPos, string placeholder)
    {
        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, yPos),
            AutoSize = true
        };

        var pnl = new Panel
        {
            Location = new Point(28, yPos + 20),
            Size = new Size(428, 34),
            BackColor = ColorCardBg,
            Padding = new Padding(8, 6, 8, 4)
        };
        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.2f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 4);
            e.Graphics.DrawPath(pen, path);
        };

        var txt = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            BackColor = ColorCardBg,
            PlaceholderText = placeholder
        };

        pnl.Controls.Add(txt);
        Controls.AddRange(new Control[] { lbl, pnl });

        yPos += 62;
        return txt;
    }

    private async Task LoadPackagesAsync()
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

            UpdatePackageDetailsLabel();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load subscription tiers: {ex.Message}", "Catalog Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdatePackageDetailsLabel()
    {
        if (cboPackages.SelectedItem is not SubscriptionPackage pkg) return;

        var feats = new List<string>();
        if (pkg.HasLoyalty) feats.Add("Loyalty");
        if (pkg.HasAnalytics) feats.Add("Analytics");
        if (pkg.HasMultiBranch) feats.Add("Multi-Branch");

        lblPackageDetails.Text = $"₱{pkg.MonthlyFee:N0}/mo • Max {pkg.MaxBranches} branch(es) • Features: {(feats.Count > 0 ? string.Join(", ", feats) : "Core only")}";
    }

    private async Task ExecuteProvisioningAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCompanyCode.Text) ||
            string.IsNullOrWhiteSpace(txtCompanyName.Text) ||
            string.IsNullOrWhiteSpace(txtBranchName.Text) ||
            string.IsNullOrWhiteSpace(txtCity.Text))
        {
            MessageBox.Show("Please fill out all required fields marked with *.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (cboPackages.SelectedValue is not int selectedPkgId)
        {
            MessageBox.Show("Please select an active CRM subscription package.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnProvision.Enabled = false;
            btnCancel.Enabled = false;
            btnProvision.Text = "Creating SQL DB...";
            lblStatus.Text = "Allocating SQL Database & executing schema...";

            await _provisioningService.ProvisionTenantAsync(
                txtCompanyCode.Text.Trim(),
                txtCompanyName.Text.Trim(),
                selectedPkgId,
                txtBranchName.Text.Trim(),
                txtCity.Text.Trim());

            lblStatus.Text = "Boutique tenant successfully provisioned!";
            MessageBox.Show(
                $"Tenant '{txtCompanyName.Text.Trim()}' provisioned successfully!\n\n" +
                $"• Database: DB_Tenant_{txtCompanyCode.Text.Trim().ToUpperInvariant()}\n" +
                $"• Assigned Plan: {((SubscriptionPackage)cboPackages.SelectedItem).PackageName}",
                "Provisioning Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Provisioning failed: {ex.GetBaseException().Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnProvision.Enabled = true;
            btnCancel.Enabled = true;
            btnProvision.Text = "Provision Database";
            lblStatus.Text = "";
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