using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CRM.infrastructure.data;
using CRM.winforms.Models;
using CRM.winforms.Views;
using Microsoft.EntityFrameworkCore;

namespace CRM.winforms.Forms;

public partial class MainForm : Form
{
    private readonly LoginResult _user;
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _currentCompanyId = 1;

    private UserControl? _activeView;
    private AnalyticsAndReportsView? _reportsView;
    private CustomerProfilesView? _customerProfilesView;
    private RentalBookingsView? _bookingsView;
    private CatalogView? _catalogView;

    // Colors
    public static readonly Color ColorSidebar = Color.FromArgb(52, 30, 33);
    public static readonly Color ColorSidebarActive = Color.FromArgb(190, 110, 120);

    public MainForm() : this(new LoginResult { Username = "DesignerStaff", Roles = new[] { "admin" } })
    {
    }

    public MainForm(LoginResult authenticatedUser)
    {
        _user = authenticatedUser ?? throw new ArgumentNullException(nameof(authenticatedUser));

        // DbContext factory matching the connection used across the application
        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer("Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;")
                .Options;
            return new TenantCrmDbContext(options);
        };

        InitializeComponent();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
        {
            ShowDashboardView();
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

    // Main views
    public void ShowDashboardView()
    {
        _reportsView ??= new AnalyticsAndReportsView(_contextFactory, () => _currentCompanyId);
        SwitchView(_reportsView);

        _ = _reportsView.LoadDashboardDataAsync();
    }
    public void ShowCustomerProfiles()
    {
        _customerProfilesView ??= new CustomerProfilesView();
        SwitchView(_customerProfilesView);
    }

    public void ShowRentalBookingsView()
    {
        _bookingsView ??= new RentalBookingsView(_contextFactory, () => _currentCompanyId);
        SwitchView(_bookingsView);
    }

    public void ShowCatalogView()
    {
        _catalogView ??= new CatalogView();
        SwitchView(_catalogView);
    }

    // Button click handlers
    private void ButtonDashboard_Click(object? sender, EventArgs e)
    {
        ShowDashboardView();
    }

    private void ButtonCustomers_Click(object sender, EventArgs e)
    {
        ShowCustomerProfiles();
    }

    private void ButtonRentals_Click(object sender, EventArgs e)
    {
        ShowRentalBookingsView();
    }

    private void ButtonCatalog_Click(object sender, EventArgs e)
    {
        ShowCatalogView();
    }

    private void Panel1_Paint(object sender, PaintEventArgs e) { }
    private void Logo_Click(object sender, EventArgs e) { }
    private void PanelContents_Paint(object sender, PaintEventArgs e) { }
}