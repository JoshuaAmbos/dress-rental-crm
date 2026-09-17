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
    private CustomerProfilesView? _customerProfilesView;
    private DashboardView? _dashboardView;

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
            ShowCustomerProfiles();
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

    public void ShowCustomerProfiles()
    {
        _customerProfilesView ??= new CustomerProfilesView();
        SwitchView(_customerProfilesView);
    }

    public void ShowDashboardView()
    {
        _dashboardView ??= new DashboardView();
        SwitchView(_dashboardView);
    }

    private void button1_Click(object sender, EventArgs e)
    {
        ShowCustomerProfiles();
    }

    private void button2_Click(object sender, EventArgs e)
    {
    }

    private void buttonDashboard_Click(object sender, EventArgs e)
    {
        ShowDashboardView();
    }

    private void panel1_Paint(object sender, PaintEventArgs e) { }
    private void logo_Click(object sender, EventArgs e) { }
    private void panelContents_Paint(object sender, PaintEventArgs e) { }
}