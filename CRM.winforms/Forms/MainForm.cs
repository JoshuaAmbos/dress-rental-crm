using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Models;
using CRM.winforms.Properties;
using CRM.winforms.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace CRM.winforms.Forms;

public partial class MainForm : Form
{
    private readonly LoginResult _user;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _currentCompanyId = 1;

    // View Caching
    private UserControl? _activeView;
    private AnalyticsAndReportsView? _reportsView;
    private CustomerProfilesView? _customerProfilesView;
    private RentalBookingsView? _bookingsView;
    private CatalogView? _catalogView;
    private InquiriesView? _inquiriesView;

    // UI Structure Controls
    private Panel pnlSidebar = null!;
    private Panel pnlTopBar = null!;
    private Panel panelContents = null!;
    private Label lblDate = null!;

    // Nav Item Tracking
    private readonly List<Button> _navButtons = new();
    private Button? _currentActiveNavButton;

    // Atelier Palette
    private static readonly Color ColorCanvasBg = Color.FromArgb(250, 245, 245);
    private static readonly Color ColorSidebarBg = Color.White;
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorActivePillBg = Color.FromArgb(253, 241, 242);
    private static readonly Color ColorNavInactiveText = Color.FromArgb(115, 105, 110);
    private static readonly Color ColorMutedLabel = Color.FromArgb(150, 140, 145);
    private static readonly Color ColorBorder = Color.FromArgb(234, 224, 224);

    public MainForm() : this(new LoginResult { Username = "Sophia Laurent", Roles = new[] { "Boutique Manager" } })
    {
    }

    public MainForm(LoginResult authenticatedUser)
    {
        _user = authenticatedUser ?? throw new ArgumentNullException(nameof(authenticatedUser));

        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(options);
        };

        InitializeComponent();
        BuildAtelierShell();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            ShowCustomerProfiles();
        }
    }

    private void BuildAtelierShell()
    {
        DoubleBuffered = true;
        BackColor = ColorCanvasBg;

        // 1. Left Navigation Sidebar
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

        // 2. Main Viewport Area Wrapper
        var pnlMainArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCanvasBg
        };
        Controls.Add(pnlMainArea);
        pnlMainArea.BringToFront();

        // 3. Top Status Bar
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

        // 4. Content Area for Injected UserControls
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
        // SECTION A: TOP FIXED SECTION (Brand -> Tenant -> Section Tag)
        var pnlTopSection = new Panel
        {
            Dock = DockStyle.Top,
            Height = 136,
            BackColor = Color.Transparent
        };

        // 1. Brand / Logo
        var pnlBrand = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(198, 48), // Increased slightly to 48px height to comfortably fit the 42px icon and stacked text
            BackColor = Color.Transparent
        };

        var picIcon = new PictureBox
        {
            Size = new Size(42, 42),
            Location = new Point(0, 3), // Centered vertically against the 48px panel
            Image = Properties.Resources.ProjectCRM_LogoNoText,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };

        var lblBrandName = new Label
        {
            Text = "Atelier",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(50, 2), // 8px gap after the 42px icon
            AutoSize = true
        };

        var lblBrandSub = new Label
        {
            Text = "CRM",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(52, 26), // Pushed below the Atelier baseline to prevent overlap
            AutoSize = true
        };

        pnlBrand.Controls.AddRange(new Control[] { picIcon, lblBrandName, lblBrandSub });

        // 2. Tenant Button ("Main Showroom ▾")
        var pnlTenant = new Panel
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

        var lblTenantBadge = new Label
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

        var lblTenantName = new Label
        {
            Text = "Main Showroom  ▾",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(34, 9),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        pnlTenant.Controls.AddRange(new Control[] { lblTenantBadge, lblTenantName });

        // 3. Navigation Header Label
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

        // SECTION B: BOTTOM FIXED SECTION (User Profile)
        var pnlUser = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White,
            Padding = new Padding(0, 8, 0, 0)
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
            TextAlign = ContentAlignment.MiddleCenter
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
            AutoSize = true
        };
        var lblUserRole = new Label
        {
            Text = _user.Roles.Length > 0 ? _user.Roles[0] : "Staff",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = ColorMutedLabel,
            Location = new Point(41, 27),
            AutoSize = true
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
        pnlUser.Controls.AddRange(new Control[] { avatar, lblUserName, lblUserRole, lblMore });

        // SECTION C: MIDDLE NAVIGATION BUTTONS CONTAINER
        var pnlNavList = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 0)
        };

        // Create Navigation Items in reverse dock order so they appear top-to-bottom
        var btnAnalytics = CreateNavButton("📊", "Analytics", (s, e) => ShowDashboardView());
        var btnInquiries = CreateNavButton("💬", "Inquiries", (s, e) => ShowInquiryView());
        var btnLoyalty = CreateNavButton("🎗", "Loyalty Awards", (s, e) => { /* Awards */ });
        var btnCatalog = CreateNavButton("👗", "Garment Catalog", (s, e) => ShowCatalogView());
        var btnRentals = CreateNavButton("📅", "Rental Pipeline", (s, e) => ShowRentalBookingsView());
        var btnCustomers = CreateNavButton("👥", "Client Directory", (s, e) => ShowCustomerProfiles());

        // Adding docked controls in reverse order produces the correct top-down sequence:
        // Client Directory -> Rental Pipeline -> Garment Catalog -> Loyalty Awards -> Inquiries -> Analytics
        pnlNavList.Controls.AddRange(new Control[]
        {
            btnAnalytics,
            btnInquiries,
            btnLoyalty,
            btnCatalog,
            btnRentals,
            btnCustomers
        });

        // Add containers to pnlSidebar and enforce strict docking order
        pnlSidebar.Controls.Add(pnlNavList);
        pnlSidebar.Controls.Add(pnlUser);
        pnlSidebar.Controls.Add(pnlTopSection);

        pnlTopSection.BringToFront();
        pnlUser.SendToBack();
        pnlNavList.BringToFront();
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

    // --- View Navigation Routers ---

    public void ShowCustomerProfiles()
    {
        _customerProfilesView ??= new CustomerProfilesView();
        SwitchView(_customerProfilesView);
        HighlightNavByText("Client Directory");
    }

    public void ShowRentalBookingsView()
    {
        _bookingsView ??= new RentalBookingsView(_contextFactory, () => _currentCompanyId);
        SwitchView(_bookingsView);
        HighlightNavByText("Rental Pipeline");
    }

    public void ShowCatalogView()
    {
        _catalogView ??= new CatalogView();
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
        _reportsView ??= new AnalyticsAndReportsView(_contextFactory, () => _currentCompanyId);
        SwitchView(_reportsView);
        _ = _reportsView.LoadDashboardDataAsync();
        HighlightNavByText("Analytics");
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