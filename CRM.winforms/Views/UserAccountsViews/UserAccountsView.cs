using CRM.infrastructure.data;
using CRM.winforms.Forms;
using CRM.winforms.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM.winforms.Views;

public partial class UserAccountsView : UserControl
{
    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly Func<TenantCrmDbContext> _tenantDbFactory;
    private readonly int _currentCompanyId;
    private readonly string _currentCompanyName;
    private readonly bool _isSuperAdmin;
    private readonly UserAccountService _userService;

    private DataGridView dgvUsers = null!;
    private Button btnCreateUser = null!;
    private Button btnEditUser = null!;
    private Button btnResetPassword = null!;
    private Button btnDeleteUser = null!;
    private Button btnMoveBranch = null!;
    private TextBox txtSearch = null!;

    private static readonly Color ColorEspresso  = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext   = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder    = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorCardBg    = Color.White;
    private static readonly Color ColorViewBg    = Color.FromArgb(250, 245, 245);

    // Primary 5-parameter constructor
    public UserAccountsView(
        Func<MasterCrmDbContext> masterDbFactory,
        Func<TenantCrmDbContext> tenantDbFactory,
        int currentCompanyId,
        string currentCompanyName,
        bool isSuperAdmin)
    {
        _masterDbFactory = masterDbFactory ?? throw new ArgumentNullException(nameof(masterDbFactory));
        _tenantDbFactory = tenantDbFactory ?? throw new ArgumentNullException(nameof(tenantDbFactory));
        _currentCompanyId = currentCompanyId;
        _currentCompanyName = currentCompanyName;
        _isSuperAdmin = isSuperAdmin;
        _userService = new UserAccountService();

        BuildLayout();
        _ = LoadUsersAsync();
    }

    // Defensive 4-parameter chaining constructor (prevents compilation breakages)
    public UserAccountsView(
        Func<MasterCrmDbContext> masterDbFactory,
        int currentCompanyId,
        string currentCompanyName,
        bool isSuperAdmin)
        : this(
            masterDbFactory,
            () => new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options),
            currentCompanyId,
            currentCompanyName,
            isSuperAdmin)
    {
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = _isSuperAdmin ? "Global User Accounts (Cross-Tenant)" : $"Staff & Manager Accounts — {_currentCompanyName}",
            Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(0, 0),
            AutoSize = true
        };

        btnCreateUser = new Button
        {
            Text = "+ New User Account",
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlHeader.Width - 160, 2),
            Cursor = Cursors.Hand
        };
        btnCreateUser.FlatAppearance.BorderSize = 0;
        btnCreateUser.Click += BtnCreateUser_Click;

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, btnCreateUser });

        // Action Toolbar
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.Transparent
        };

        txtSearch = new TextBox
        {
            PlaceholderText = "Search by username, email, role, or branch...",
            Location = new Point(0, 10),
            Width = 280,
            Font = new Font("Segoe UI", 9.5f)
        };
        txtSearch.TextChanged += async (s, e) => await LoadUsersAsync(txtSearch.Text.Trim());

        btnEditUser = CreateActionButton("✏️ Edit User", 290, 96, async (s, e) => await EditSelectedUserAsync());
        btnResetPassword = CreateActionButton("🔑 Reset Password", 392, 130, async (s, e) => await ResetSelectedPasswordAsync());
        btnDeleteUser = CreateActionButton("🗑 Delete", 528, 86, async (s, e) => await DeleteSelectedUserAsync());
        btnMoveBranch = CreateActionButton("🏢 Move Branch", 620, 120, async (s, e) => await MoveSelectedUserBranchAsync());

        pnlToolbar.Controls.AddRange(new Control[] { txtSearch, btnEditUser, btnResetPassword, btnDeleteUser, btnMoveBranch });

        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(12)
        };
        pnlGrid.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlGrid.Width - 1, pnlGrid.Height - 1), 8);
            using var pen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(pen, path);
        };

        dgvUsers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(242, 235, 235),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 38 }
        };
        dgvUsers.EnableHeadersVisualStyles = false;
        dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(253, 248, 248);
        dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = ColorEspresso;
        dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold);
        dgvUsers.ColumnHeadersHeight = 36;

        pnlGrid.Controls.Add(dgvUsers);

        Controls.Add(pnlGrid);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
    }

    private Button CreateActionButton(string text, int left, int width, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(left, 8),
            Size = new Size(width, 32),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            BackColor = Color.FromArgb(245, 240, 240),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += onClick;
        return btn;
    }

    public async Task LoadUsersAsync(string query = "")
    {
        try
        {
            await using var db = _masterDbFactory();

            var rawUsers = await (from u in db.Users
                                  join ur in db.UserRoles on u.Id equals ur.UserId into userRoles
                                  from ur in userRoles.DefaultIfEmpty()
                                  join r in db.Roles on ur.RoleId equals r.Id into roles
                                  from r in roles.DefaultIfEmpty()
                                  join uc in db.UserClaims.Where(c => c.ClaimType == "CompanyId") on u.Id equals uc.UserId into companyClaims
                                  from uc in companyClaims.DefaultIfEmpty()
                                  join un in db.UserClaims.Where(c => c.ClaimType == "CompanyName") on u.Id equals un.UserId into nameClaims
                                  from un in nameClaims.DefaultIfEmpty()
                                  join ub in db.UserClaims.Where(c => c.ClaimType == "BranchName") on u.Id equals ub.UserId into branchClaims
                                  from ub in branchClaims.DefaultIfEmpty()
                                  select new
                                  {
                                      u.Id,
                                      u.UserName,
                                      u.Email,
                                      Role = r != null ? r.Name : "Staff",
                                      CompanyId = uc != null ? uc.ClaimValue : "1",
                                      CompanyName = un != null ? un.ClaimValue : "Atelier Haute Couture",
                                      Branch = ub != null ? ub.ClaimValue : "All Showrooms"
                                  }).ToListAsync();

            var list = rawUsers;

            if (!_isSuperAdmin)
            {
                list = list.Where(u => u.CompanyId == _currentCompanyId.ToString()).ToList();
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                list = list.Where(u =>
                    (u.UserName != null && u.UserName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Role != null && u.Role.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Branch != null && u.Branch.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            dgvUsers.DataSource = list.Select(x => new
            {
                UserId = x.Id,
                Username = x.UserName,
                Email = x.Email,
                Role = x.Role,
                ShowroomBranch = x.Branch,
                CompanyId = int.TryParse(x.CompanyId, out var cid) ? cid : 1,
                CompanyName = x.CompanyName,
                BoutiqueTenant = $"{x.CompanyName} (#{x.CompanyId})"
            }).ToList();

            if (dgvUsers.Columns.Contains("UserId")) dgvUsers.Columns["UserId"]!.Visible = false;
            if (dgvUsers.Columns.Contains("CompanyId")) dgvUsers.Columns["CompanyId"]!.Visible = false;
            if (dgvUsers.Columns.Contains("CompanyName")) dgvUsers.Columns["CompanyName"]!.Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load users: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnCreateUser_Click(object? sender, EventArgs e)
    {
        using var dialog = new CreateUserDialogForm(_masterDbFactory, _currentCompanyId, _currentCompanyName, _isSuperAdmin);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadUsersAsync();
        }
    }

    private async Task MoveSelectedUserBranchAsync()
    {
        if (dgvUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a user to move.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = dgvUsers.SelectedRows[0];
        string userId = row.Cells["UserId"].Value?.ToString() ?? "";
        string username = row.Cells["Username"].Value?.ToString() ?? "";
        string currentBranch = row.Cells["ShowroomBranch"].Value?.ToString() ?? "All Showrooms";
        int companyId = row.Cells["CompanyId"].Value is int cid ? cid : _currentCompanyId;
        string companyName = row.Cells["CompanyName"].Value?.ToString() ?? _currentCompanyName;

        if (username.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Superadmin does not belong to an individual showroom.", "Protected Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // Query branches for this tenant
            await using var db = _tenantDbFactory();
            var branches = await db.Branches
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId && b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            if (branches.Count == 0)
            {
                MessageBox.Show(
                    $"This boutique tenant ({companyName}) does not have any showroom branches configured in the database.",
                    "No Branches Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using var moveDialog = new MoveBranchDialogForm(userId, username, currentBranch, branches);
            if (moveDialog.ShowDialog(this) == DialogResult.OK)
            {
                await LoadUsersAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error querying branches: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EditSelectedUserAsync()
    {
        if (dgvUsers.SelectedRows.Count == 0) return;

        var row = dgvUsers.SelectedRows[0];
        string userId = row.Cells["UserId"].Value?.ToString() ?? "";
        string username = row.Cells["Username"].Value?.ToString() ?? "";
        string email = row.Cells["Email"].Value?.ToString() ?? "";
        string role = row.Cells["Role"].Value?.ToString() ?? "Staff";
        int companyId = row.Cells["CompanyId"].Value is int cid ? cid : 1;
        string companyName = row.Cells["CompanyName"].Value?.ToString() ?? "Atelier Haute Couture";

        if (username.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("The primary Superadmin account cannot be altered.", "Protected Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var editForm = new EditUserDialogForm(userId, username, email, role, companyId, companyName, _isSuperAdmin);
        if (editForm.ShowDialog(this) == DialogResult.OK)
        {
            await LoadUsersAsync();
        }
    }

    private async Task ResetSelectedPasswordAsync()
    {
        if (dgvUsers.SelectedRows.Count == 0) return;

        var row = dgvUsers.SelectedRows[0];
        string userId = row.Cells["UserId"].Value?.ToString() ?? "";
        string username = row.Cells["Username"].Value?.ToString() ?? "";

        using var prompt = new Form
        {
            Text = $"Reset Password: {username}",
            Size = new Size(380, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };

        var lbl = new Label { Text = "New Temporary Password:", Left = 20, Top = 20, AutoSize = true, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold) };
        var txt = new TextBox { Left = 20, Top = 45, Width = 320, Text = "TempPass@2026!" };
        var btn = new Button { Text = "Reset Password", Left = 200, Top = 95, Width = 140, Height = 34, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = ColorDustyRose, DialogResult = DialogResult.OK };
        btn.FlatAppearance.BorderSize = 0;
        prompt.Controls.AddRange(new Control[] { lbl, txt, btn });

        if (prompt.ShowDialog(this) == DialogResult.OK)
        {
            var (success, msg) = await _userService.ResetPasswordAsync(userId, txt.Text.Trim());
            MessageBox.Show(msg, success ? "Success" : "Error", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }

    private async Task DeleteSelectedUserAsync()
    {
        if (dgvUsers.SelectedRows.Count == 0) return;

        var row = dgvUsers.SelectedRows[0];
        string userId = row.Cells["UserId"].Value?.ToString() ?? "";
        string username = row.Cells["Username"].Value?.ToString() ?? "";

        if (username.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("The primary Superadmin account cannot be deleted.", "Protected Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you sure you want to permanently delete account '{username}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            var (success, msg) = await _userService.DeleteUserAsync(userId);
            if (success)
            {
                await LoadUsersAsync();
            }
            else
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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