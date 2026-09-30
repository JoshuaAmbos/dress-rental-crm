using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM.winforms.Controls;
using Microsoft.EntityFrameworkCore;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class TenantDialogForm : Form
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly TenantProvisioningService _provisioningService;

    private TextBox txtCode = null!;
    private TextBox txtName = null!;
    private ComboBox cboPackage = null!;
    private TextBox txtBranch = null!;
    private TextBox txtCity = null!;
    private PrimaryButton btnSave = null!;

    public TenantDialogForm(
        Func<MasterCrmDbContext> masterDbFactory,
        TenantProvisioningService provisioningService)
    {
        _masterDbFactory = masterDbFactory;
        _provisioningService = provisioningService;

        Text = "Provision New Tenant Boutique";
        Size = new Size(460, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorDialogBg;
        Font = new Font("Segoe UI", 9.5f);

        BuildControls();
        _ = LoadPackagesAsync();
    }

    private void BuildControls()
    {
        var lblHeader = new Label
        {
            Text = "New Boutique Tenant",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(24, 20),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Creates an isolated SQL database and seeds default showroom.",
            ForeColor = ColorSubtext,
            Location = new Point(26, 46),
            AutoSize = true
        };

        int y = 84;
        AddFormField("Company Code (e.g., CEBU01):", ref txtCode, ref y);
        AddFormField("Boutique Legal Name:", ref txtName, ref y);

        // Package Combo
        var lblPkg = new Label { Text = "Subscription Package:", Location = new Point(26, y), AutoSize = true, ForeColor = ColorEspresso };
        cboPackage = new ComboBox { Location = new Point(26, y + 22), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList };
        Controls.AddRange(new Control[] { lblPkg, cboPackage });
        y += 56;

        AddFormField("Flagship Showroom Name:", ref txtBranch, ref y, "Flagship Atelier");
        AddFormField("City / Location:", ref txtCity, ref y, "Metro Manila");

        btnSave = new PrimaryButton
        {
            Text = "Provision Database & Register",
            Location = new Point(26, y + 10),
            Size = new Size(390, 40)
        };
        btnSave.Click += async (s, e) => await HandleProvisionAsync();

        Controls.AddRange(new Control[] { lblHeader, lblSub, btnSave });
    }

    private void AddFormField(string labelText, ref TextBox tb, ref int y, string defaultValue = "")
    {
        var lbl = new Label { Text = labelText, Location = new Point(26, y), AutoSize = true, ForeColor = ColorEspresso };
        tb = new TextBox { Location = new Point(26, y + 22), Width = 390, Text = defaultValue };
        Controls.AddRange(new Control[] { lbl, tb });
        y += 56;
    }

    private async Task LoadPackagesAsync()
    {
        await using var db = _masterDbFactory();
        var pkgs = await db.SubscriptionPackages.Where(p => p.IsActive).ToListAsync();
        cboPackage.DataSource = pkgs;
        cboPackage.DisplayMember = "PackageName";
        cboPackage.ValueMember = "SubscriptionPackageId";
    }

    private async Task HandleProvisionAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Please fill out both the Company Code and Company Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSave.Enabled = false;
        btnSave.Text = "Creating SQL Database... Please wait...";

        try
        {
            int pkgId = (int)(cboPackage.SelectedValue ?? 1);
            await _provisioningService.ProvisionTenantAsync(
                txtCode.Text.Trim(),
                txtName.Text.Trim(),
                pkgId,
                string.IsNullOrWhiteSpace(txtBranch.Text) ? "Flagship Atelier" : txtBranch.Text.Trim(),
                string.IsNullOrWhiteSpace(txtCity.Text) ? "Main City" : txtCity.Text.Trim()
            );

            MessageBox.Show("Tenant provisioned successfully with isolated database and initial showroom.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Provisioning failed: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
            btnSave.Text = "Provision Database & Register";
        }
    }
}