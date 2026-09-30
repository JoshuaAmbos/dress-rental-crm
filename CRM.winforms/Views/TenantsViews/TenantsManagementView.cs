using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class TenantsManagementView : UserControl
{
    private readonly Func<MasterCrmDbContext> _masterContextFactory;
    private readonly TenantProvisioningService _provisioningService;
    private DataGridView _dgvTenants = null!;
    private TextBox _txtSearch = null!;

    public TenantsManagementView(
        Func<MasterCrmDbContext> masterContextFactory,
        TenantProvisioningService provisioningService)
    {
        _masterContextFactory = masterContextFactory ?? throw new ArgumentNullException(nameof(masterContextFactory));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));

        BuildViewLayout();
    }

    private void BuildViewLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);

        // Header Panel
        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "Tenant & Boutique Directory",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Provision dedicated tenant databases, assign subscription packages, and control boutique access.",
            Font = new Font("Segoe UI", 9.75f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Action Toolbar
        var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };

        var btnAddTenant = new PrimaryButton
        {
            Text = "+ Provision New Tenant",
            Size = new Size(190, 36),
            Location = new Point(0, 6)
        };
        btnAddTenant.Click += async (s, e) => await OpenCreateTenantDialogAsync();

        var btnRefresh = new SecondaryButton
        {
            Text = "↻ Refresh",
            Size = new Size(95, 36),
            Location = new Point(200, 6)
        };
        btnRefresh.Click += async (s, e) => await LoadTenantsAsync();

        _txtSearch = new TextBox
        {
            Size = new Size(240, 32),
            Location = new Point(Width - 272, 8),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.5f)
        };
        _txtSearch.TextChanged += (s, e) => FilterGrid(_txtSearch.Text);

        pnlToolbar.Controls.AddRange(new Control[] { btnAddTenant, btnRefresh, _txtSearch });

        // Card Container with ModernDataGridView
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

        _dgvTenants = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        ConfigureColumns();
        pnlCard.Controls.Add(_dgvTenants);

        Controls.Add(pnlCard);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);

        _dgvTenants.CellContentClick += async (s, e) =>
        {
            if (e.RowIndex < 0) return;
            var companyId = (int)_dgvTenants.Rows[e.RowIndex].Cells["CompanyId"].Value;

            if (_dgvTenants.Columns[e.ColumnIndex].Name == "ColAction")
            {
                await ToggleTenantStatusAsync(companyId);
            }
        };

        _ = LoadTenantsAsync();
    }

    private void ConfigureColumns()
    {
        _dgvTenants.Columns.Clear();
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyId", HeaderText = "ID", Width = 50, Visible = false });
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyCode", HeaderText = "CODE", Width = 110 });
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "CompanyName", HeaderText = "BOUTIQUE / TENANT NAME", FillWeight = 160 });
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Package", HeaderText = "SUBSCRIPTION", Width = 160 });
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Database", HeaderText = "ISOLATED DATABASE", Width = 180 });
        _dgvTenants.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", Width = 100 });

        var colBtn = new DataGridViewButtonColumn
        {
            Name = "ColAction",
            HeaderText = "ACTION",
            Text = "Deactivate",
            UseColumnTextForButtonValue = false,
            Width = 110,
            FlatStyle = FlatStyle.Flat
        };
        _dgvTenants.Columns.Add(colBtn);
    }

    public async Task LoadTenantsAsync()
    {
        try
        {
            await using var db = _masterContextFactory();
            var companies = await db.Companies
                .Include(c => c.SubscriptionPackage)
                .Include(c => c.CompanyDatabases)
                .AsNoTracking()
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.CompanyName)
                .ToListAsync();

            _dgvTenants.Rows.Clear();
            foreach (var c in companies)
            {
                var dbName = c.CompanyDatabases.FirstOrDefault(d => d.IsActive)?.DatabaseName ?? "Not Assigned";
                var pkgName = c.SubscriptionPackage?.PackageName ?? "No Plan";
                var rowIndex = _dgvTenants.Rows.Add(
                    c.CompanyId,
                    c.CompanyCode,
                    c.CompanyName,
                    pkgName,
                    dbName,
                    c.IsActive ? "Active" : "Deactivated",
                    c.IsActive ? "Deactivate" : "Reactivate"
                );

                if (!c.IsActive)
                {
                    _dgvTenants.Rows[rowIndex].DefaultCellStyle.ForeColor = ColorSubtext;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load tenants: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FilterGrid(string query)
    {
        foreach (DataGridViewRow row in _dgvTenants.Rows)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                row.Visible = true;
                continue;
            }

            var code = row.Cells["CompanyCode"].Value?.ToString() ?? "";
            var name = row.Cells["CompanyName"].Value?.ToString() ?? "";
            var db = row.Cells["Database"].Value?.ToString() ?? "";

            row.Visible = code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                          name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                          db.Contains(query, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task ToggleTenantStatusAsync(int companyId)
    {
        var confirm = MessageBox.Show(
            "Are you sure you want to change this tenant's activation status? Deactivated tenants cannot be accessed by staff.",
            "Confirm State Change",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = _masterContextFactory();
            var tenant = await db.Companies.FindAsync(companyId);
            if (tenant == null) return;

            tenant.IsActive = !tenant.IsActive;
            tenant.DeactivatedAt = tenant.IsActive ? null : DateTime.UtcNow;

            var dbs = await db.CompanyDatabases.Where(d => d.CompanyId == companyId).ToListAsync();
            foreach (var d in dbs) d.IsActive = tenant.IsActive;

            await db.SaveChangesAsync();
            await LoadTenantsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenCreateTenantDialogAsync()
    {
        using var dlg = new TenantDialogForm(_masterContextFactory, _provisioningService);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            await LoadTenantsAsync();
        }
    }
}