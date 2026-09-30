using CRM.domain.entities;
using CRM.winforms.Services;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public partial class MoveBranchDialogForm : Form
{
    private readonly string _userId;
    private readonly string _username;
    private readonly List<Branch> _branches;
    private readonly string _currentBranchName;
    private readonly UserAccountService _userService;

    private ComboBox cmbBranches = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public MoveBranchDialogForm(
        string userId,
        string username,
        string currentBranchName,
        List<Branch> branches)
    {
        _userId = userId;
        _username = username;
        _currentBranchName = currentBranchName;
        _branches = branches ?? [];
        _userService = new UserAccountService();

        BuildUI();
        PopulateBranches();
    }

    private void BuildUI()
    {
        Text = $"Reassign Showroom: {_username}";
        Size = new Size(420, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.25f);

        var lblHeader = new Label
        {
            Text = $"Move Showroom: {_username}",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(24, 18),
            AutoSize = true
        };

        var lblCurrent = new Label
        {
            Text = $"CURRENT SHOWROOM: {_currentBranchName.ToUpperInvariant()}",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(24, 48),
            AutoSize = true
        };

        var lblPrompt = new Label
        {
            Text = "SELECT DESTINATION SHOWROOM / BRANCH",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(24, 80),
            AutoSize = true
        };

        cmbBranches = new ComboBox
        {
            Location = new Point(24, 102),
            Width = 356,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(184, 170),
            Size = new Size(90, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = ColorPrimary,
            BackColor = Color.FromArgb(245, 240, 240),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        btnSave = new Button
        {
            Text = "Move Staff",
            Location = new Point(280, 170),
            Size = new Size(100, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ColorAccent,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveBranchAssignmentAsync();

        Controls.AddRange(new Control[] { lblHeader, lblCurrent, lblPrompt, cmbBranches, btnCancel, btnSave });
    }

    private void PopulateBranches()
    {
        cmbBranches.Items.Clear();

        // Option to unassign / allow staff to roam
        cmbBranches.Items.Add(new BranchItem(null, "All Showrooms (Unassigned / Roving)"));

        int selectedIndex = 0;
        int index = 1;

        foreach (var b in _branches)
        {
            var item = new BranchItem(b.BranchId, b.BranchName, b.City);
            cmbBranches.Items.Add(item);

            if (_currentBranchName.Contains(b.BranchName, StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = index;
            }
            index++;
        }

        cmbBranches.SelectedIndex = selectedIndex;
    }

    private async Task SaveBranchAssignmentAsync()
    {
        if (cmbBranches.SelectedItem is not BranchItem selected) return;

        btnSave.Enabled = false;
        btnSave.Text = "Moving...";

        var (success, msg) = await _userService.MoveUserBranchAsync(_userId, selected.BranchId, selected.BranchName);

        if (success)
        {
            MessageBox.Show(msg, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
            btnSave.Text = "Move Staff";
        }
    }

    private record BranchItem(int? BranchId, string BranchName, string City = "")
    {
        public override string ToString() =>
            string.IsNullOrWhiteSpace(City) ? BranchName : $"🏢 {BranchName} ({City})";
    }
}