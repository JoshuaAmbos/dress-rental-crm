using CRM.infrastructure.data;
using CRM.winforms.Forms;
using CRM.winforms.Services;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class UserAccountsView : UserControl
{
    private const string DefaultTenantConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<MasterCrmDbContext> _masterDbFactory;
    private readonly Func<TenantCrmDbContext> _tenantDbFactory;
    private readonly int _currentCompanyId;
    private readonly string _currentCompanyName;
    private readonly bool _isSuperAdmin;
    private readonly UserAccountService _userService;

    // Header & Actions
    private Label lblTitle = null!;
    private Label lblSub = null!;
    private Button btnCreateUser = null!;

    // Toolbar Controls
    private TextBox txtSearch = null!;
    private Button btnEditUser = null!;
    private Button btnResetPassword = null!;
    private Button btnDeleteUser = null!;
    private Button btnMoveBranch = null!;
    private Label lblRecordsCount = null!;

    // Grid Container
    private DataGridView dgvUsers = null!;

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

        InitializeLayout();
        _ = LoadUsersAsync();
    }

    // 4-parameter chaining constructor (prevents compilation breakages across view callers)
    public UserAccountsView(
        Func<MasterCrmDbContext> masterDbFactory,
        int currentCompanyId,
        string currentCompanyName,
        bool isSuperAdmin)
        : this(
            masterDbFactory,
            () => new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(DefaultTenantConnectionString)
                .Options),
            currentCompanyId,
            currentCompanyName,
            isSuperAdmin)
    {
    }

    private void InitializeLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Zone 1: Header & Primary Action
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.Transparent
        };

        lblTitle = new Label
        {
            Text = _isSuperAdmin ? "Global User Accounts (Cross-Tenant)" : $"Staff & Manager Accounts — {_currentCompanyName}",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblSub = new Label
        {
            Text = "Provision staff credentials, manage RBAC security roles, and assign showroom branches.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        btnCreateUser = new Button
        {
            Text = "+ New User Account",
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(165, 38),
            Cursor = Cursors.Hand
        };
        btnCreateUser.FlatAppearance.BorderSize = 0;
        btnCreateUser.MouseEnter += (s, e) => btnCreateUser.BackColor = ColorAccentHover;
        btnCreateUser.MouseLeave += (s, e) => btnCreateUser.BackColor = ColorAccent;
        btnCreateUser.Click += BtnCreateUser_Click;

        pnlHeader.Resize += (s, e) =>
        {
            btnCreateUser.Location = new Point(Math.Max(0, pnlHeader.ClientSize.Width - btnCreateUser.Width), 12);
        };
        btnCreateUser.Location = new Point(Math.Max(0, pnlHeader.ClientSize.Width - btnCreateUser.Width), 12);

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnCreateUser });

        // 2. Zone 2: Toolbar & Management Controls
        var pnlToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };

        var pnlSearch = new Panel
        {
            Location = new Point(0, 4),
            Size = new Size(280, 34),
            BackColor = ColorCardBg
        };
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        txtSearch = new TextBox
        {
            BorderStyle = BorderStyle.None,
            PlaceholderText = "Search by username, email, role, or branch...",
            Location = new Point(10, 8),
            Width = 260,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark
        };
        txtSearch.TextChanged += async (s, e) => await LoadUsersAsync(txtSearch.Text.Trim());
        pnlSearch.Controls.Add(txtSearch);

        btnEditUser = CreateActionButton("✏️ Edit User", 290, 100, async (s, e) => await EditSelectedUserAsync());
        btnResetPassword = CreateActionButton("🔑 Reset Password", 398, 135, async (s, e) => await ResetSelectedPasswordAsync());
        btnDeleteUser = CreateActionButton("🗑 Delete", 541, 88, async (s, e) => await DeleteSelectedUserAsync());
        btnMoveBranch = CreateActionButton("🏢 Move Branch", 637, 125, async (s, e) => await MoveSelectedUserBranchAsync());

        lblRecordsCount = new Label
        {
            Dock = DockStyle.Right,
            Text = "0 accounts",
            ForeColor = ColorSubtext,
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 8, 0, 0)
        };

        pnlToolbar.Controls.AddRange(new Control[] { pnlSearch, btnEditUser, btnResetPassword, btnDeleteUser, btnMoveBranch, lblRecordsCount });

        // 3. Zone 3: Data Card Canvas
        var pnlGridWrapper = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 4),
            BackColor = Color.Transparent
        };

        var pnlCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(1)
        };
        pnlCard.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
        };

        dgvUsers = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ColorCardBg,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = ColorDivider,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowTemplate = { Height = 42 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 40,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        };

        dgvUsers.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            SelectionBackColor = ColorCardBg,
            SelectionForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0)
        };

        dgvUsers.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        dgvUsers.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorRowAlt,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(10, 0, 10, 0)
        };

        ConfigureGridColumns();
        dgvUsers.CellPainting += DgvUsers_CellPainting;

        pnlCard.Controls.Add(dgvUsers);
        pnlGridWrapper.Controls.Add(pnlCard);

        Controls.Add(pnlGridWrapper);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
    }

    private void ConfigureGridColumns()
    {
        dgvUsers.Columns.Clear();

        var boldStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold) };

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserId", DataPropertyName = "UserId", Visible = false });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", DataPropertyName = "CompanyId", Visible = false });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", DataPropertyName = "CompanyName", Visible = false });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Username",
            DataPropertyName = "Username",
            HeaderText = "USERNAME",
            Width = 150,
            DefaultCellStyle = boldStyle
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Email",
            DataPropertyName = "Email",
            HeaderText = "EMAIL ADDRESS",
            Width = 200,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Role",
            DataPropertyName = "Role",
            HeaderText = "ROLE",
            Width = 130
        });

        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ShowroomBranch",
            DataPropertyName = "ShowroomBranch",
            HeaderText = "SHOWROOM BRANCH",
            Width = 180
        });

        if (_isSuperAdmin)
        {
            dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BoutiqueTenant",
                DataPropertyName = "BoutiqueTenant",
                HeaderText = "BOUTIQUE TENANT",
                Width = 220
            });
        }

        foreach (DataGridViewColumn col in dgvUsers.Columns)
        {
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    private Button CreateActionButton(string text, int left, int width, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(left, 4),
            Size = new Size(width, 34),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
            ForeColor = ColorBrandDark,
            BackColor = ColorCardBg,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = ColorBorder;
        btn.MouseEnter += (s, e) =>
        {
            btn.BackColor = ColorActivePill;
            btn.FlatAppearance.BorderColor = ColorAccent;
        };
        btn.MouseLeave += (s, e) =>
        {
            btn.BackColor = ColorCardBg;
            btn.FlatAppearance.BorderColor = ColorBorder;
        };
        btn.Click += onClick;
        return btn;
    }

    private void DgvUsers_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;

        // Custom Render Role Pill Badges
        if (dgvUsers.Columns[e.ColumnIndex].Name == "Role" && e.Value is string role)
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isRowSelected = (e.State & DataGridViewElementStates.Selected) != 0;

            var (bg, fg) = role switch
            {
                "Superadmin" or "Super Admin" => (ColorActivePill, ColorAccent),
                "Admin" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
                "Manager" or "Boutique Manager" => (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235)),
                _ => (ColorBadgeBg, ColorSuccess)
            };

            if (isRowSelected)
            {
                bg = Color.White;
            }

            var rect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + 9, 100, 24);
            using (var brush = new SolidBrush(bg))
            using (var path = CreateRoundedRectangle(rect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                role,
                new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
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

            dgvUsers.AutoGenerateColumns = false;
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

            dgvUsers.ClearSelection();
            lblRecordsCount.Text = $"{list.Count} accounts registered";
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
        if (dgvUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a user account to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

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
        if (dgvUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select an account to reset password.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = dgvUsers.SelectedRows[0];
        string userId = row.Cells["UserId"].Value?.ToString() ?? "";
        string username = row.Cells["Username"].Value?.ToString() ?? "";

        using var prompt = new Form
        {
            Text = $"Reset Password: {username}",
            Size = new Size(380, 210),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = ColorCardBg
        };

        var lbl = new Label
        {
            Text = "New Temporary Password:",
            Left = 24,
            Top = 20,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorBrandDark
        };

        var txt = new TextBox
        {
            Left = 24,
            Top = 46,
            Width = 316,
            Font = new Font("Segoe UI", 9.5f),
            Text = "TempPass@2026!"
        };

        var btn = new Button
        {
            Text = "Reset Password",
            Left = 200,
            Top = 105,
            Width = 140,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = ColorAccent,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            DialogResult = DialogResult.OK,
            Cursor = Cursors.Hand
        };
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
        if (dgvUsers.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select an account to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

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
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}