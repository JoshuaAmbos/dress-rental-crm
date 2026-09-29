using CRM.domain.Constants;
using CRM.infrastructure.data;
using CRM.winforms.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CRM.winforms.Forms;

public partial class CreateUserDialogForm : Form
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly int _currentCompanyId;
    private readonly string _currentCompanyName;
    private readonly bool _isSuperAdmin;
    private readonly UserAccountService _userService;

    private TextBox txtUsername = null!;
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private Button btnShowPassword = null!;
    private ComboBox cmbRole = null!;
    private ComboBox cmbTenant = null!;
    private Label lblPasswordFeedback = null!;
    private Button btnSubmit = null!;
    private Button btnCancel = null!;

    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorError = Color.FromArgb(200, 50, 60);

    public CreateUserDialogForm(Func<MasterCrmDbContext> masterDbFactory, int companyId, string companyName, bool isSuperAdmin)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _currentCompanyId = companyId;
        _currentCompanyName = companyName;
        _isSuperAdmin = isSuperAdmin;
        _userService = new UserAccountService();

        BuildUI();
        _ = LoadTenantsAsync();
    }

    private void BuildUI()
    {
        Text = "User Account Provisioning";
        Size = new Size(460, 530);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

        var lblHeader = new Label
        {
            Text = "Provision Boutique User Account",
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(24, 18),
            AutoSize = true
        };

        // Username
        var lblUser = new Label { Text = "USERNAME", Location = new Point(24, 56), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        txtUsername = new TextBox { Location = new Point(24, 76), Width = 394, Font = new Font("Segoe UI", 9.5f) };

        // Email
        var lblEmail = new Label { Text = "EMAIL ADDRESS", Location = new Point(24, 112), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        txtEmail = new TextBox { Location = new Point(24, 132), Width = 394, Font = new Font("Segoe UI", 9.5f) };

        // Password & Show Password Button
        var lblPass = new Label { Text = "PASSWORD (STRICT COMPLEXITY)", Location = new Point(24, 168), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        txtPassword = new TextBox { Location = new Point(24, 188), Width = 340, UseSystemPasswordChar = true, Font = new Font("Segoe UI", 9.5f) };
        txtPassword.TextChanged += (s, e) => ValidatePasswordRules();

        btnShowPassword = new Button
        {
            Text = "👁",
            Location = new Point(370, 187),
            Size = new Size(48, 26),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f),
            ForeColor = ColorEspresso,
            BackColor = Color.FromArgb(245, 240, 240),
            Cursor = Cursors.Hand
        };
        btnShowPassword.FlatAppearance.BorderSize = 0;
        btnShowPassword.Click += (s, e) =>
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            btnShowPassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🔒";
        };

        lblPasswordFeedback = new Label
        {
            Text = "Must be ≥ 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char.",
            Font = new Font("Segoe UI", 7.75f),
            ForeColor = ColorSubtext,
            Location = new Point(24, 216),
            AutoSize = true
        };

        // Role Dropdown
        var lblRole = new Label { Text = "ASSIGNED ROLE", Location = new Point(24, 242), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        cmbRole = new ComboBox { Location = new Point(24, 262), Width = 394, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbRole.Items.AddRange(new object[] { AppRoles.Staff, AppRoles.Manager, AppRoles.Admin });
        cmbRole.SelectedItem = AppRoles.Staff;

        // Boutique Tenant Dropdown
        var lblTenant = new Label { Text = "BOUTIQUE TENANT", Location = new Point(24, 298), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        cmbTenant = new ComboBox { Location = new Point(24, 318), Width = 394, DropDownStyle = ComboBoxStyle.DropDownList };

        // Buttons
        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(222, 410),
            Size = new Size(90, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ColorEspresso,
            BackColor = Color.FromArgb(245, 240, 240),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        btnSubmit = new Button
        {
            Text = "Create User",
            Location = new Point(318, 410),
            Size = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSubmit.FlatAppearance.BorderSize = 0;
        btnSubmit.Click += async (s, e) => await SubmitCreationAsync();

        Controls.AddRange(new Control[]
        {
            lblHeader,
            lblUser, txtUsername,
            lblEmail, txtEmail,
            lblPass, txtPassword, btnShowPassword, lblPasswordFeedback,
            lblRole, cmbRole,
            lblTenant, cmbTenant,
            btnCancel, btnSubmit
        });
    }

    private async Task LoadTenantsAsync()
    {
        cmbTenant.Items.Clear();

        if (_isSuperAdmin)
        {
            bool loadedFromDb = false;
            try
            {
                await using var db = _masterDbFactory();
                var companies = await db.Companies.AsNoTracking().ToListAsync();
                if (companies.Count > 0)
                {
                    foreach (var c in companies)
                    {
                        cmbTenant.Items.Add(new TenantItem(c.CompanyId, c.CompanyName));
                    }
                    loadedFromDb = true;
                }
            }
            catch { }

            // Safe fallback if Master DB Companies table is unpopulated
            if (!loadedFromDb)
            {
                cmbTenant.Items.Add(new TenantItem(1, "Atelier Haute Couture"));
                cmbTenant.Items.Add(new TenantItem(2, "Maison Étoile Bridal"));
                cmbTenant.Items.Add(new TenantItem(3, "Davao Haute Rentals"));
            }

            cmbTenant.SelectedIndex = 0;
            cmbTenant.Enabled = true;
        }
        else
        {
            // Store Admins are locked strictly to their own boutique
            string companyName = string.IsNullOrWhiteSpace(_currentCompanyName) ? "Atelier Haute Couture" : _currentCompanyName;
            cmbTenant.Items.Add(new TenantItem(_currentCompanyId, companyName));
            cmbTenant.SelectedIndex = 0;
            cmbTenant.Enabled = false;
        }
    }

    private bool ValidatePasswordRules()
    {
        string p = txtPassword.Text;
        bool hasMinLength = p.Length >= 8;
        bool hasUpper = Regex.IsMatch(p, @"[A-Z]");
        bool hasLower = Regex.IsMatch(p, @"[a-z]");
        bool hasDigit = Regex.IsMatch(p, @"\d");
        bool hasSpecial = Regex.IsMatch(p, @"[^a-zA-Z\d]");

        bool isValid = hasMinLength && hasUpper && hasLower && hasDigit && hasSpecial;

        if (isValid)
        {
            lblPasswordFeedback.Text = "✓ Meets strict complexity rules.";
            lblPasswordFeedback.ForeColor = Color.FromArgb(40, 140, 60);
        }
        else
        {
            lblPasswordFeedback.Text = "Must be ≥ 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char.";
            lblPasswordFeedback.ForeColor = ColorError;
        }

        return isValid;
    }

    private async Task SubmitCreationAsync()
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
        {
            MessageBox.Show("Username and Email are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidatePasswordRules())
        {
            MessageBox.Show("Password does not meet complexity requirements.", "Weak Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Focus();
            return;
        }

        var selectedRole = cmbRole.SelectedItem?.ToString() ?? AppRoles.Staff;

        int compId = _currentCompanyId;
        string compName = string.IsNullOrWhiteSpace(_currentCompanyName) ? "Atelier Haute Couture" : _currentCompanyName;

        if (cmbTenant.SelectedItem is TenantItem item)
        {
            compId = item.CompanyId;
            compName = item.CompanyName;
        }

        btnSubmit.Enabled = false;
        btnSubmit.Text = "Creating...";

        var (success, message) = await _userService.RegisterUserAsync(
            txtUsername.Text.Trim(),
            txtEmail.Text.Trim(),
            txtPassword.Text,
            selectedRole,
            compId,
            compName);

        if (success)
        {
            MessageBox.Show($"Account '{txtUsername.Text.Trim()}' provisioned successfully under {compName}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(message, "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSubmit.Enabled = true;
            btnSubmit.Text = "Create User";
        }
    }

    public record TenantItem(int CompanyId, string CompanyName)
    {
        public override string ToString() => $"Company #{CompanyId} — {CompanyName}";
    }
}