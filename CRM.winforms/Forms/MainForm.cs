using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CRM.winforms.Models;
using CRM.winforms.Views;

namespace CRM.winforms.Forms;

public partial class MainForm : Form
{
    private readonly LoginResult _user;
    private UserControl? _activeView;
    private DashboardView? _dashboardView;
    private CustomerProfilesView? _customerProfilesView;
    private RentalBookingsView? _bookingsView;
    private CatalogView? _catalogView;


    // colors
    public static readonly Color ColorSidebar = Color.FromArgb(52, 30, 33);
    public static readonly Color ColorSidebarActive = Color.FromArgb(190, 110, 120);

    public MainForm() : this(new LoginResult { Username = "DesignerStaff", Roles = new[] { "admin" } })
    {
    }

    public MainForm(LoginResult authenticatedUser)
    {
        _user = authenticatedUser ?? throw new ArgumentNullException(nameof(authenticatedUser));
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

    // main views
    public void ShowDashboardView()
    {
        _dashboardView ??= new DashboardView();
        SwitchView(_dashboardView);
    }

    public void ShowCustomerProfiles()
    {
        _customerProfilesView ??= new CustomerProfilesView();
        SwitchView(_customerProfilesView);
    }

    public void ShowRentalBookingsView()
    {
        //if (buttonRentals != null) SetActiveButton(buttonRentals);
        //_bookingsView ??= new RentalBookingsView(GetActiveCompanyId);
        _bookingsView ??= new RentalBookingsView();
        SwitchView(_bookingsView);
        //_ = _bookingsView.LoadPipelineDataAsync();
    }

    public void ShowCatalogView()
    {
        _catalogView ??= new CatalogView();
        SwitchView(_catalogView);
    }

    // button click handlers
    private void ButtonDashboard_Click(object sender, EventArgs e)
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