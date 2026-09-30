using CRM.domain.Constants;
using CRM.winforms.Services;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public partial class EditUserDialogForm : Form
{
    private readonly string _userId;
    private readonly string _username;
    private readonly string _currentEmail;
    private readonly string _currentRole;
    private readonly int _currentCompanyId;
    private readonly string _currentCompanyName;
    private readonly bool _isSuperAdmin;
    private readonly UserAccountService _userService;

    private TextBox txtEmail = null!;
    private ComboBox cmbRole = null!;
    private ComboBox cmbTenant = null!;
    private Button btnSubmit = null!;
    private Button btnCancel = null!;

    public EditUserDialogForm(string userId, string username, string email, string role, int companyId, string companyName, bool isSuperAdmin)
    {
        _userId = userId;
        _username = username;
        _currentEmail = email;
        _currentRole = role;
        _currentCompanyId = companyId;
        _currentCompanyName = companyName;
        _isSuperAdmin = isSuperAdmin;
        _userService = new UserAccountService();

        BuildUI();
    }

    private void BuildUI()
    {
        Text = $"Edit User: {_username}";
        Size = new Size(420, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.25f);

        var lblHeader = new Label
        {
            Text = $"Edit Profile: {_username}",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(24, 18),
            AutoSize = true
        };

        var lblEmail = new Label { Text = "EMAIL ADDRESS", Location = new Point(24, 56), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        txtEmail = new TextBox { Location = new Point(24, 76), Width = 356, Text = _currentEmail };

        var lblRole = new Label { Text = "ROLE", Location = new Point(24, 116), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        cmbRole = new ComboBox { Location = new Point(24, 136), Width = 356, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbRole.Items.AddRange(new object[] { AppRoles.Staff, AppRoles.Manager, AppRoles.Admin });
        cmbRole.SelectedItem = cmbRole.Items.Contains(_currentRole) ? _currentRole : AppRoles.Staff;

        var lblTenant = new Label { Text = "TENANT", Location = new Point(24, 176), AutoSize = true, ForeColor = ColorSubtext, Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold) };
        cmbTenant = new ComboBox { Location = new Point(24, 196), Width = 356, DropDownStyle = ComboBoxStyle.DropDownList };

        if (_isSuperAdmin)
        {
            cmbTenant.Items.Add(new CreateUserDialogForm.TenantItem(1, "Atelier Haute Couture"));
            cmbTenant.Items.Add(new CreateUserDialogForm.TenantItem(2, "Maison Étoile Bridal"));
            cmbTenant.Items.Add(new CreateUserDialogForm.TenantItem(3, "Davao Haute Rentals"));

            var match = cmbTenant.Items.OfType<CreateUserDialogForm.TenantItem>().FirstOrDefault(t => t.CompanyId == _currentCompanyId);
            cmbTenant.SelectedItem = match ?? cmbTenant.Items[0];
            cmbTenant.Enabled = true;
        }
        else
        {
            cmbTenant.Items.Add(new CreateUserDialogForm.TenantItem(_currentCompanyId, _currentCompanyName));
            cmbTenant.SelectedIndex = 0;
            cmbTenant.Enabled = false;
        }

        btnCancel = new Button { Text = "Cancel", Location = new Point(180, 260), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, ForeColor = ColorAccent, BackColor = Color.FromArgb(245, 240, 240) };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        btnSubmit = new Button { Text = "Save Changes", Location = new Point(278, 260), Size = new Size(102, 34), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = ColorPrimary, Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) };
        btnSubmit.FlatAppearance.BorderSize = 0;
        btnSubmit.Click += async (s, e) =>
        {
            var selectedTenant = (CreateUserDialogForm.TenantItem)cmbTenant.SelectedItem!;
            var (success, msg) = await _userService.UpdateUserAsync(
                _userId,
                txtEmail.Text.Trim(),
                cmbRole.SelectedItem?.ToString() ?? AppRoles.Staff,
                selectedTenant.CompanyId,
                selectedTenant.CompanyName);

            if (success)
            {
                MessageBox.Show("User profile updated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Controls.AddRange(new Control[] { lblHeader, lblEmail, txtEmail, lblRole, cmbRole, lblTenant, cmbTenant, btnCancel, btnSubmit });
    }
}