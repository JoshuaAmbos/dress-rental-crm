using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.winforms.Views;

public partial class CustomerProfilesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<int> _getCompanyId;

    public event Action<Customer>? CustomerEditRequested;

    public CustomerProfilesView() : this(() => 1)
    {
    }

    public CustomerProfilesView(Func<int> getCompanyId)
    {
        _getCompanyId = getCompanyId ?? (() => 1);
        InitializeComponent();

        if (!DesignMode)
        {
            AddActionColumns();

            primaryButtonNewCustomer?.Click += BtnNewCustomer_Click;

            dgvCustomers?.CellContentClick += DgvCustomers_CellContentClick;

            chkShowArchived?.CheckedChanged += ChkShowArchived_CheckedChanged;
        }
    }

    private void AddActionColumns()
    {
        if (dgvCustomers == null || dgvCustomers.Columns.Contains("colEdit")) return;

        var colEdit = new DataGridViewButtonColumn
        {
            Name = "colEdit",
            HeaderText = "Action",
            Text = "Edit",
            UseColumnTextForButtonValue = true,
            Width = 70,
            FlatStyle = FlatStyle.Flat
        };
        colEdit.DefaultCellStyle.BackColor = Color.FromArgb(244, 238, 238);
        colEdit.DefaultCellStyle.ForeColor = Color.FromArgb(38, 22, 24);
        colEdit.DefaultCellStyle.SelectionBackColor = Color.FromArgb(190, 110, 120);
        colEdit.DefaultCellStyle.SelectionForeColor = Color.White;

        var colArchive = new DataGridViewButtonColumn
        {
            Name = "colArchive",
            HeaderText = "",
            Text = "Archive",
            UseColumnTextForButtonValue = true,
            Width = 75,
            FlatStyle = FlatStyle.Flat
        };
        colArchive.DefaultCellStyle.BackColor = Color.FromArgb(245, 240, 235);
        colArchive.DefaultCellStyle.ForeColor = Color.FromArgb(140, 95, 60);
        colArchive.DefaultCellStyle.SelectionBackColor = Color.FromArgb(140, 95, 60);
        colArchive.DefaultCellStyle.SelectionForeColor = Color.White;

        dgvCustomers.Columns.Add(colEdit);
        dgvCustomers.Columns.Add(colArchive);
    }

    private void UpdateActionColumnMode(bool showingArchived)
    {
        if (dgvCustomers?.Columns["colArchive"] is DataGridViewButtonColumn col)
        {
            col.Text = showingArchived ? "Restore" : "Archive";
            col.DefaultCellStyle.BackColor = showingArchived
                ? Color.FromArgb(235, 245, 235)
                : Color.FromArgb(245, 240, 235);
            col.DefaultCellStyle.ForeColor = showingArchived
                ? Color.FromArgb(40, 120, 60)
                : Color.FromArgb(140, 95, 60);
        }
    }

    private async void DgvCustomers_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvCustomers.Rows.Count) return;

        var clickedColumn = dgvCustomers.Columns[e.ColumnIndex].Name;
        var row = dgvCustomers.Rows[e.RowIndex];

        if (row.DataBoundItem is not Customer customer) return;

        if (clickedColumn == "colEdit")
        {
            OnEditCustomer(customer);
        }
        else if (clickedColumn == "colArchive")
        {
            if (customer.IsActive)
            {
                await OnArchiveCustomerAsync(customer);
            }
            else
            {
                await OnRestoreCustomerAsync(customer);
            }
        }
    }

    private async void OnEditCustomer(Customer customer)
    {
        using var dialog = new CustomerDialogForm(GetDbContext, _getCompanyId(), customer);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
    }

    private async Task OnArchiveCustomerAsync(Customer customer)
    {
        var confirm = MessageBox.Show(
            $"Archive client profile for '{customer.CustomerName}'?\n\nTheir records will be preserved, but hidden from active lists.",
            "Archive Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = GetDbContext();
            var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (entity != null)
            {
                entity.IsActive = false;
                await db.SaveChangesAsync();
            }

            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnRestoreCustomerAsync(Customer customer)
    {
        var confirm = MessageBox.Show(
            $"Restore and reactivate client '{customer.CustomerName}'?",
            "Restore Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = GetDbContext();
            var entity = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (entity != null)
            {
                entity.IsActive = true;
                await db.SaveChangesAsync();
            }

            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChkShowArchived_CheckedChanged(object? sender, EventArgs e)
    {
        bool showArchived = chkShowArchived?.Checked ?? false;
        UpdateActionColumnMode(showArchived);
        _ = LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
    }

    private TenantCrmDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }

    private void CustomerProfilesView_Load(object sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode)
        {
            _ = LoadCustomerDataAsync();
        }
    }

    public async Task LoadCustomerDataAsync(string search = "")
    {
        if (DesignMode) return;

        try
        {
            await using var db = GetDbContext();
            int companyId = _getCompanyId();
            bool showArchived = chkShowArchived?.Checked ?? false;

            var query = db.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId && c.IsActive == !showArchived);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.CustomerCode.Contains(search) ||
                    c.CustomerName.Contains(search) ||
                    c.ContactNumber.Contains(search) ||
                    c.EmailAddress.Contains(search) ||
                    c.Address.Contains(search));
            }

            var list = await query
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            if (customerBindingSource != null)
            {
                customerBindingSource.DataSource = list;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customer profiles: {ex.Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnNewCustomer_Click(object? sender, EventArgs e)
    {
        using var dialog = new CustomerDialogForm(GetDbContext, _getCompanyId());

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
    }

    private void searchBar1_SearchTextChanged(object? sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            _ = LoadCustomerDataAsync(searchBar1.TextValue.Trim());
        }
    }

    private void searchBar1_Load(object sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            searchBar1.SearchTextChanged += searchBar1_SearchTextChanged;
        }
    }

    private void textBox1_TextChanged(object sender, EventArgs e) { }
    private void label1_Click(object sender, EventArgs e) { }
    private void pictureBox1_Click(object sender, EventArgs e) { }
    private void chkShowArchived_CheckedChanged_1(object sender, EventArgs e) { }
}