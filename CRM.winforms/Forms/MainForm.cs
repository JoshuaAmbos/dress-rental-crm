using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Views;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Forms;

public partial class MainForm : Form
{
    public bool IsSignedOut { get; private set; }

    private readonly LoginResult _user;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly Func<MasterCrmDbContext> _masterContextFactory;
    private readonly int _currentCompanyId;
    private ContextMenuStrip _userAccountMenu = null!;

    // Multi-Branch State Tracking
    private int? _currentBranchId = null;
    private string _currentBranchName = "All Showrooms";
    private List<Branch> _cachedBranches = [];
    private Label lblTenantBadge = null!;
    private Label lblTenantName = null!;
    private Panel pnlTenant = null!;
    private ContextMenuStrip _branchSelectorMenu = null!;

    // View Caching
    private UserControl? _activeView;
    private AnalyticsAndReportsView? _reportsView;
    private CustomerProfilesView? _customerProfilesView;
    private RentalBookingsView? _bookingsView;
    private CatalogView? _catalogView;
    private InquiriesView? _inquiriesView;
    private ComplaintsView? _complaintsView;
    private LoyaltyAwardsView? _loyaltyAwardsView;
    private TermsAndConditionsView? _termsView;
    private UserAccountsView? _usersView;
    private SystemConfigurationView? _configView;

    // UI Structure Controls
    private Panel pnlSidebar = null!;
    private Panel pnlTopBar = null!;
    private Panel panelContents = null!;
    private Label lblDate = null!;

    // Nav Item Tracking & Role-Gated Buttons
    private readonly List<Button> _navButtons = [];
    private Button? _currentActiveNavButton;
    private Button _btnCustomers = null!;
    private Button _btnRentals = null!;
    private Button _btnCatalog = null!;
    private Button _btnInquiries = null!;
    private Button _btnComplaints = null!;
    private Button _btnLoyalty = null!;
    private Button _btnAnalytics = null!;
    private Button _btnTerms = null!;
    private Button _btnUsers = null!;
    private Button _btnSettings = null!;

    // Atelier Palette
    private static readonly Color ColorCanvasBg = Color.FromArgb(250, 245, 245);
    private static readonly Color ColorSidebarBg = Color.White;
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorActivePillBg = Color.FromArgb(253, 241, 242);
    private static readonly Color ColorNavInactiveText = Color.FromArgb(115, 105, 110);
    private static readonly Color ColorMutedLabel = Color.FromArgb(150, 140, 145);
    private static readonly Color ColorBorder = Color.FromArgb(234, 224, 224);

    public MainForm() : this(new LoginResult
    {
        Username = "Sophia Laurent",
        Roles = new[] { "Manager" },
        CompanyId = 1,
        CompanyName = "Atelier Haute Couture"
    })
    {
    }

    public MainForm(LoginResult authenticatedUser)
    {
        _user = authenticatedUser ?? throw new ArgumentNullException(nameof(authenticatedUser));
        _currentCompanyId = authenticatedUser.CompanyId > 0 ? authenticatedUser.CompanyId : 1;

        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(options);
        };

        _masterContextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_MasterCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new MasterCrmDbContext(options);
        };

        InitializeComponent();
        BuildAtelierShell();
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            await LoadBranchDropdownMenuAsync();

            bool isSuperAdmin = _user.Roles.Any(r =>
                r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
                r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

            // Super Admin lands on User Accounts; store roles land on Customer Profiles
            if (isSuperAdmin)
            {
                ShowUsersView();
            }
            else
            {
                ShowCustomerProfiles();
            }
        }
    }

    private void BuildAtelierShell()
    {
        DoubleBuffered = true;
        BackColor = ColorCanvasBg;

        // Left Navigation Sidebar
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = ColorSidebarBg,
            Padding = new Padding(16, 18, 16, 16)
        };

        pnlSidebar.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawLine(pen, pnlSidebar.Width - 1, 0, pnlSidebar.Width - 1, pnlSidebar.Height);
        };

        BuildSidebarContents();
        Controls.Add(pnlSidebar);

        // Apply role visibility directly after building the sidebar
        ApplyRolePermissions();

        // Main Content Area Wrapper
        var pnlMainArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCanvasBg
        };
        Controls.Add(pnlMainArea);
        pnlMainArea.BringToFront();

        // Top Status Bar
        pnlTopBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = ColorSidebarBg,
            Padding = new Padding(32, 16, 32, 0)
        };

        lblDate = new Label
        {
            Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy"),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorMutedLabel,
            Location = new Point(32, 16),
            AutoSize = true
        };
        pnlTopBar.Controls.Add(lblDate);
        pnlMainArea.Controls.Add(pnlTopBar);

        // Content Area for Injected Views
        panelContents = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCanvasBg
        };
        pnlMainArea.Controls.Add(panelContents);
        panelContents.BringToFront();
    }

    private void BuildSidebarContents()
    {
        // Top Fixed Brand Section
        var pnlTopSection = new Panel
        {
            Dock = DockStyle.Top,
            Height = 136,
            BackColor = Color.Transparent
        };

        var pnlBrand = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(198, 48),
            BackColor = Color.Transparent
        };

        var picIcon = new PictureBox
        {
            Size = new Size(42, 42),
            Location = new Point(0, 3),
            Image = Properties.Resources.ProjectCRM_LogoNoText,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        var lblBrandName = new Label
        {
            Text = string.IsNullOrWhiteSpace(_user.CompanyName) ? "Atelier" : _user.CompanyName,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(50, 2),
            AutoSize = true
        };

        var lblBrandSub = new Label
        {
            Text = "CRM",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(52, 26),
            AutoSize = true
        };

        pnlBrand.Controls.AddRange(new Control[] { picIcon, lblBrandName, lblBrandSub });

        // Branch and Showroom Selector Dropdown
        _branchSelectorMenu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9f),
            ShowImageMargin = false
        };

        pnlTenant = new Panel
        {
            Location = new Point(0, 52),
            Size = new Size(198, 36),
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };
        pnlTenant.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, pnlTenant.Width - 1, pnlTenant.Height - 1), 7);
            e.Graphics.DrawPath(pen, path);
        };

        lblTenantBadge = new Label
        {
            Text = "M",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            Size = new Size(20, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(8, 8),
            Cursor = Cursors.Hand
        };
        lblTenantBadge.Paint += (s, e) =>
        {
            using var path = GraphicsHelper.CreateRoundedRectangle(new Rectangle(0, 0, 19, 19), 4);
            lblTenantBadge.Region = new Region(path);
        };

        lblTenantName = new Label
        {
            Text = "All Showrooms  ▾",
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(34, 9),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        EventHandler openBranchMenu = (s, e) =>
        {
            _branchSelectorMenu.Show(pnlTenant, new Point(0, pnlTenant.Height + 2));
        };

        pnlTenant.Click += openBranchMenu;
        lblTenantBadge.Click += openBranchMenu;
        lblTenantName.Click += openBranchMenu;

        pnlTenant.Controls.AddRange(new Control[] { lblTenantBadge, lblTenantName });

        // Navigation Header Label
        var lblNavTag = new Label
        {
            Text = "NAVIGATION",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(2, 108),
            Size = new Size(196, 22),
            TextAlign = ContentAlignment.BottomLeft
        };

        pnlTopSection.Controls.AddRange(new Control[] { pnlBrand, pnlTenant, lblNavTag });

        // Bottom User Profile Section
        var pnlUser = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White,
            Padding = new Padding(0, 8, 0, 0),
            Cursor = Cursors.Hand
        };
        pnlUser.Paint += (s, e) =>
        {
            using var pen = new Pen(ColorBorder, 1f);
            e.Graphics.DrawLine(pen, 0, 0, pnlUser.Width, 0);
        };

        var avatar = new Label
        {
            Text = GetInitials(_user.Username),
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            Size = new Size(32, 32),
            Location = new Point(2, 12),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        avatar.Paint += (s, e) =>
        {
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, 31, 31);
            avatar.Region = new Region(path);
        };

        var lblUserName = new Label
        {
            Text = _user.Username,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(40, 10),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        var lblUserRole = new Label
        {
            Text = _user.Roles.Length > 0 ? _user.Roles[0] : "Staff",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = ColorMutedLabel,
            Location = new Point(41, 27),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        var lblMore = new Label
        {
            Text = "···",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(170, 14),
            AutoSize = true,
            Cursor = Cursors.Hand
        };

        // User Account Popup Menu
        _userAccountMenu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 9.5f),
            ShowImageMargin = false
        };

        var itemAccountHeader = new ToolStripMenuItem($"👤  {_user.Username}") { Enabled = false };
        var itemTenantHeader = new ToolStripMenuItem($"🏢  {_user.CompanyName}") { Enabled = false };

        var itemProfile = new ToolStripMenuItem("⚙️  Account Profile", null, (s, e) =>
        {
            MessageBox.Show(
                $"User ID: {_user.UserId}\n" +
                $"Username: {_user.Username}\n" +
                $"Email: {_user.Email}\n" +
                $"Role: {string.Join(", ", _user.Roles)}\n\n" +
                $"Tenant: {_user.CompanyName} (Company ID: {_currentCompanyId})\n" +
                $"Active Showroom: {_currentBranchName}",
                "Account Profile",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        var itemSignOut = new ToolStripMenuItem("🚪  Sign Out", null, (s, e) =>
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to sign out?",
                "Confirm Sign Out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                IsSignedOut = true;
                this.Close();
            }
        });

        _userAccountMenu.Items.AddRange(new ToolStripItem[]
        {
            itemAccountHeader,
            itemTenantHeader,
            new ToolStripSeparator(),
            itemProfile,
            new ToolStripSeparator(),
            itemSignOut
        });

        EventHandler openUserMenu = (s, e) =>
        {
            _userAccountMenu.Show(pnlUser, new Point(0, 0), ToolStripDropDownDirection.AboveRight);
        };

        pnlUser.Click += openUserMenu;
        avatar.Click += openUserMenu;
        lblUserName.Click += openUserMenu;
        lblUserRole.Click += openUserMenu;
        lblMore.Click += openUserMenu;

        pnlUser.Controls.AddRange(new Control[] { avatar, lblUserName, lblUserRole, lblMore });

        // Navigation Buttons Container
        var pnlNavList = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };

        //TODO: Replace emoji icons with line icons
        _btnSettings = CreateNavButton("⚙️", "System Config", (s, e) => ShowSystemConfigView());
        _btnUsers = CreateNavButton("👥", "User Accounts", (s, e) => ShowUsersView());
        _btnTerms = CreateNavButton("📜", "Terms & Conditions", (s, e) => ShowTermsView());
        _btnAnalytics = CreateNavButton("📊", "Analytics", (s, e) => ShowDashboardView());
        _btnLoyalty = CreateNavButton("🎗", "Loyalty Awards", (s, e) => ShowLoyaltyAwardsView());
        _btnInquiries = CreateNavButton("💬", "Inquiries", (s, e) => ShowInquiryView());
        _btnComplaints = CreateNavButton("⚠️", "Complaints", (s, e) => ShowComplaintsView());
        _btnCatalog = CreateNavButton("👗", "Garment Catalog", (s, e) => ShowCatalogView());
        _btnRentals = CreateNavButton("📅", "Rental Pipeline", (s, e) => ShowRentalBookingsView());
        _btnCustomers = CreateNavButton("👤", "Client Directory", (s, e) => ShowCustomerProfiles());

        pnlNavList.Controls.AddRange(new Control[]
        {
            _btnSettings,
            _btnUsers,
            _btnTerms,
            _btnAnalytics,
            _btnLoyalty,
            _btnComplaints, // <-- Added here
            _btnInquiries,
            _btnCatalog,
            _btnRentals,
            _btnCustomers
        });

        pnlSidebar.Controls.Add(pnlNavList);
        pnlSidebar.Controls.Add(pnlTopSection);
        pnlSidebar.Controls.Add(pnlUser);

        pnlTopSection.BringToFront();
        pnlUser.BringToFront();
        pnlNavList.BringToFront();
    }

    private void ApplyRolePermissions()
    {
        // 1. Role identification flags
        bool isSuperAdmin = _user.Roles.Any(r =>
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

        bool isAdmin = _user.Roles.Any(r =>
            r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

        bool isManager = _user.Roles.Any(r =>
            r.Equals(AppRoles.Manager, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Boutique Manager", StringComparison.OrdinalIgnoreCase));

        bool isStaff = _user.Roles.Any(r =>
            r.Equals(AppRoles.Staff, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Sales Staff", StringComparison.OrdinalIgnoreCase));

        // 2. Operational Front-Desk & Loyalty (Staff, Manager, Admin)
        bool hasFrontDeskAccess = isStaff || isManager || isAdmin;
        _btnCustomers.Visible = hasFrontDeskAccess;
        _btnRentals.Visible = hasFrontDeskAccess;
        _btnCatalog.Visible = hasFrontDeskAccess;
        _btnInquiries.Visible = hasFrontDeskAccess;
        _btnComplaints.Visible = hasFrontDeskAccess;
        _btnLoyalty.Visible = hasFrontDeskAccess;

        // 3. Oversight & Analytics (Manager and Admin only)
        _btnAnalytics.Visible = isManager || isAdmin;

        // 4. Terms and Conditions (All 4 roles)
        _btnTerms.Visible = true;

        // 5. User Accounts Management (Admin and Super Admin)
        _btnUsers.Visible = isAdmin || isSuperAdmin;

        // 6. System Configuration Management (Super Admin ONLY)
        _btnSettings.Visible = isSuperAdmin;

        // 7. Showroom Switching Lock (Managers and Admins)
        pnlTenant.Enabled = isManager || isAdmin;
        pnlTenant.Cursor = (isManager || isAdmin) ? Cursors.Hand : Cursors.Default;
    }

    private async Task LoadBranchDropdownMenuAsync()
    {
        try
        {
            await using var db = _contextFactory();
            _cachedBranches = await db.Branches
                .AsNoTracking()
                .Where(b => b.CompanyId == _currentCompanyId && b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            _branchSelectorMenu.Items.Clear();

            var itemAll = new ToolStripMenuItem("🌐  All Showrooms", null, (s, e) => SelectBranch(null, "All Showrooms", "A"));
            _branchSelectorMenu.Items.Add(itemAll);
            _branchSelectorMenu.Items.Add(new ToolStripSeparator());

            foreach (var b in _cachedBranches)
            {
                string badgeInitial = string.IsNullOrEmpty(b.City) ? "B" : b.City.Substring(0, 1).ToUpperInvariant();
                var item = new ToolStripMenuItem($"🏢  {b.BranchName}", null, (s, e) => SelectBranch(b.BranchId, b.BranchName, badgeInitial));
                _branchSelectorMenu.Items.Add(item);
            }
        }
        catch { }
    }
    public void ShowComplaintsView()
    {
        _complaintsView ??= new ComplaintsView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_complaintsView);
        _ = _complaintsView.LoadComplaintsAsync();
        HighlightNavByText("Complaints");
    }

    private void SelectBranch(int? branchId, string branchName, string initial)
    {
        _currentBranchId = branchId;
        _currentBranchName = branchName;

        lblTenantBadge.Text = initial;
        lblTenantName.Text = branchName.Length > 16 ? $"{branchName.Substring(0, 14)}... ▾" : $"{branchName} ▾";

        if (_activeView is ComplaintsView)
        {
            _complaintsView = null;
            ShowComplaintsView();
        }
        else if (_activeView is CustomerProfilesView)
        {
            _customerProfilesView = null;
            ShowCustomerProfiles();
        }
        else if (_activeView is RentalBookingsView)
        {
            _bookingsView = null;
            ShowRentalBookingsView();
        }
        else if (_activeView is CatalogView)
        {
            _catalogView = null;
            ShowCatalogView();
        }
        else if (_activeView is AnalyticsAndReportsView)
        {
            _reportsView = null;
            ShowDashboardView();
        }
        else if (_activeView is LoyaltyAwardsView)
        {
            _loyaltyAwardsView = null;
            ShowLoyaltyAwardsView();
        }
    }

    private Button CreateNavButton(string icon, string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Dock = DockStyle.Top,
            Height = 40,
            Text = $"  {icon}   {text}",
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular),
            ForeColor = ColorNavInactiveText,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 2)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(253, 248, 248);

        btn.Click += (s, e) =>
        {
            SetActiveNavButton(btn);
            onClick(s, e);
        };

        _navButtons.Add(btn);
        return btn;
    }

    private void SetActiveNavButton(Button activeBtn)
    {
        _currentActiveNavButton = activeBtn;

        foreach (var btn in _navButtons)
        {
            bool isActive = (btn == activeBtn);
            btn.BackColor = isActive ? ColorActivePillBg : Color.Transparent;
            btn.ForeColor = isActive ? ColorDustyRose : ColorNavInactiveText;
            btn.Font = new Font("Segoe UI", 9.25f, isActive ? FontStyle.Bold : FontStyle.Regular);
            btn.Invalidate();
        }
    }

    private void SwitchView(UserControl view)
    {
        if (_activeView == view || panelContents == null) return;

        panelContents.SuspendLayout();
        panelContents.Controls.Clear();
        view.Dock = DockStyle.Fill;
        panelContents.Controls.Add(view);
        view.BringToFront();
        panelContents.ResumeLayout();

        _activeView = view;
    }

    // View Navigation Routers

    public void ShowCustomerProfiles()
    {
        _customerProfilesView = new CustomerProfilesView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_customerProfilesView);
        HighlightNavByText("Client Directory");
    }

    public void ShowRentalBookingsView()
    {
        _bookingsView ??= new RentalBookingsView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_bookingsView);
        HighlightNavByText("Rental Pipeline");
    }

    public void ShowCatalogView()
    {
        _catalogView ??= new CatalogView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_catalogView);
        HighlightNavByText("Garment Catalog");
    }

    public void ShowInquiryView()
    {
        _inquiriesView ??= new InquiriesView(_contextFactory, () => _currentCompanyId);
        SwitchView(_inquiriesView);
        _ = _inquiriesView.LoadInquiriesAsync();
        HighlightNavByText("Inquiries");
    }

    public void ShowDashboardView()
    {
        _reportsView ??= new AnalyticsAndReportsView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_reportsView);
        _ = _reportsView.LoadDashboardDataAsync();
        HighlightNavByText("Analytics");
    }

    public void ShowLoyaltyAwardsView()
    {
        _loyaltyAwardsView = new LoyaltyAwardsView(_contextFactory, () => _currentCompanyId, () => _currentBranchId);
        SwitchView(_loyaltyAwardsView);
        HighlightNavByText("Loyalty Awards");
    }

    public void ShowTermsView()
    {
        _termsView ??= new TermsAndConditionsView(_contextFactory, _user.Roles);
        SwitchView(_termsView);
        HighlightNavByText("Terms & Conditions");
    }

    public void ShowUsersView()
    {
        bool isSuperAdmin = _user.Roles.Any(r =>
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

        _usersView ??= new UserAccountsView(
            _masterContextFactory,
            _contextFactory, // <-- Passed here
            _currentCompanyId,
            string.IsNullOrWhiteSpace(_user.CompanyName) ? "Atelier Haute Couture" : _user.CompanyName,
            isSuperAdmin);

        SwitchView(_usersView);
        HighlightNavByText("User Accounts");
    }

    public void ShowSystemConfigView()
    {
        bool isSuperAdmin = _user.Roles.Any(r =>
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase));

        if (!isSuperAdmin)
        {
            MessageBox.Show("Access Denied: System Configuration is restricted to Super Admin only.", "Unauthorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configView ??= new SystemConfigurationView(_contextFactory);
        SwitchView(_configView);
        HighlightNavByText("System Config");
    }

    private void HighlightNavByText(string labelSub)
    {
        var match = _navButtons.Find(b => b.Text.Contains(labelSub));
        if (match != null && match != _currentActiveNavButton)
        {
            SetActiveNavButton(match);
        }
    }

    private static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "SL";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }
}