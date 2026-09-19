using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Forms;

namespace CRM.winforms.Views;

public partial class CustomerProfilesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<int> _getCompanyId;

    public CustomerProfilesView() : this(() => 1)
    {
    }

    public CustomerProfilesView(Func<int> getCompanyId)
    {
        _getCompanyId = getCompanyId ?? (() => 1);
        InitializeComponent();

        searchBar1.SetCueBanner("Search by customer name, conta...");

        if (!DesignMode)
        {
            AddActionColumns();

            if (primaryButtonNewCustomer != null)
                primaryButtonNewCustomer.Click += BtnNewCustomer_Click;

            if (dgvCustomers != null)
            {
                dgvCustomers.CellContentClick -= DgvCustomers_CellContentClick;
                dgvCustomers.CellContentClick += DgvCustomers_CellContentClick;
            }

            if (chkShowArchived != null)
                chkShowArchived.CheckedChanged += ChkShowArchived_CheckedChanged;

            _ = LoadCustomerDataAsync();
        }
    }

    private void CustomerProfilesView_Load(object sender, EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode)
        {
            _ = LoadCustomerDataAsync();
        }
    }

    private void AddActionColumns()
    {
        if (dgvCustomers == null) return;

        dgvCustomers.Columns.Clear();
        dgvCustomers.AutoGenerateColumns = false;

        // 1. Client Code
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Code",
            HeaderText = "CODE",
            FillWeight = 85,
            MinimumWidth = 80
        });

        // 2. Client Name
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Name",
            HeaderText = "NAME",
            FillWeight = 125,
            MinimumWidth = 110
        });

        // 3. Contact Number
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Phone",
            HeaderText = "PHONE",
            FillWeight = 95,
            MinimumWidth = 90
        });

        // 4. Email Address
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Email",
            HeaderText = "EMAIL ADDRESS",
            FillWeight = 135,
            MinimumWidth = 120
        });

        // 5. City / Location
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Address",
            HeaderText = "CITY / ADDRESS",
            FillWeight = 110,
            MinimumWidth = 100
        });

        // 6. Measurements
        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BustSize",
            HeaderText = "BUST SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "WaistSize",
            HeaderText = "WAIST SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "HipSize",
            HeaderText = "HIP SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

        // 7. Edit Action (Widened to 96px to prevent header clipping)
        var colEdit = new DataGridViewButtonColumn
        {
            Name = "colEdit",
            HeaderText = "ACTIONS",
            Text = "Edit",
            UseColumnTextForButtonValue = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
            Width = 96,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

        // 8. Archive / Restore Action
        var colArchive = new DataGridViewButtonColumn
        {
            Name = "colArchive",
            HeaderText = "",
            Text = "Archive",
            UseColumnTextForButtonValue = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
            Width = 88,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };

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

        var clickedColumn = dgvCustomers.Columns[e.ColumnIndex]?.Name;
        if (clickedColumn == null) return;

        var row = dgvCustomers.Rows[e.RowIndex];

        // Safely extract the Customer entity from either direct binding or anonymous projection
        Customer? customer = row.DataBoundItem as Customer;
        if (customer == null && row.DataBoundItem != null)
        {
            var prop = row.DataBoundItem.GetType().GetProperty("CustomerEntity");
            customer = prop?.GetValue(row.DataBoundItem) as Customer;
        }

        if (customer == null) return;

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
        var displayName = $"{customer.FirstName} {customer.LastName}".Trim();
        var confirm = MessageBox.Show(
            $"Archive client profile for '{displayName}'?\n\nTheir records will be preserved, but hidden from active lists.",
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
        var displayName = $"{customer.FirstName} {customer.LastName}".Trim();
        var confirm = MessageBox.Show(
            $"Restore and reactivate client '{displayName}'?",
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
                    c.FirstName.Contains(search) ||
                    c.LastName.Contains(search) ||
                    (c.ContactNumber != null && c.ContactNumber.Contains(search)) ||
                    (c.EmailAddress != null && c.EmailAddress.Contains(search)) ||
                    (c.Address != null && c.Address.Contains(search)));
            }

            var customers = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            var displayList = customers.Select(c =>
            {
                string fullName = $"{c.FirstName} {c.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = !string.IsNullOrWhiteSpace(c.FirstName) ? c.FirstName : $"Client ({c.CustomerCode})";
                }

                return new
                {
                    CustomerEntity = c,
                    Code = c.CustomerCode,
                    Name = fullName,
                    Phone = string.IsNullOrWhiteSpace(c.ContactNumber) ? "—" : c.ContactNumber,
                    Email = string.IsNullOrWhiteSpace(c.EmailAddress) ? "—" : c.EmailAddress,
                    Address = string.IsNullOrWhiteSpace(c.Address) ? "—" : c.Address,
                    BustSize = c.BustSize > 0 ? $"{c.BustSize:0.#} in" : "—",
                    WaistSize = c.WaistSize > 0 ? $"{c.WaistSize:0.#} in" : "—",
                    HipSize = c.HipSize > 0 ? $"{c.HipSize:0.#} in" : "—"
                };
            }).ToList();

            if (dgvCustomers != null)
            {
                dgvCustomers.DataSource = displayList;
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

    private void SearchBar1_SearchTextChanged(object? sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            _ = LoadCustomerDataAsync(searchBar1.TextValue.Trim());
        }
    }

    private void SearchBar1_Load(object sender, EventArgs e)
    {
        searchBar1?.SearchTextChanged += SearchBar1_SearchTextChanged;
    }
}