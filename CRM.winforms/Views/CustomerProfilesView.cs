using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Forms;
using CRM.winforms.Models;
using CRM.winforms.Services;

namespace CRM.winforms.Views;

public partial class CustomerProfilesView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly CustomerProfileService _customerService;
    private readonly Func<int> _getCompanyId;

    public CustomerProfilesView() : this(() => 1)
    {
    }

    public CustomerProfilesView(Func<int> getCompanyId)
    {
        _getCompanyId = getCompanyId ?? (() => 1);
        _contextFactory = () =>
        {
            var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new TenantCrmDbContext(options);
        };
        _customerService = new CustomerProfileService(_contextFactory);

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

    private void CustomerProfilesView_Load(object? sender, EventArgs e)
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

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Code),
            HeaderText = "CODE",
            FillWeight = 85,
            MinimumWidth = 80
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Name),
            HeaderText = "NAME",
            FillWeight = 125,
            MinimumWidth = 110
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Phone),
            HeaderText = "PHONE",
            FillWeight = 95,
            MinimumWidth = 90
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Email),
            HeaderText = "EMAIL ADDRESS",
            FillWeight = 135,
            MinimumWidth = 120
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.Address),
            HeaderText = "CITY / ADDRESS",
            FillWeight = 110,
            MinimumWidth = 100
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.BustSize),
            HeaderText = "BUST SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.WaistSize),
            HeaderText = "WAIST SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

        dgvCustomers.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(CustomerRowViewModel.HipSize),
            HeaderText = "HIP SIZE",
            FillWeight = 75,
            MinimumWidth = 70
        });

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
        if (row.DataBoundItem is not CustomerRowViewModel item) return;

        if (clickedColumn == "colEdit")
        {
            OnEditCustomer(item.CustomerEntity);
        }
        else if (clickedColumn == "colArchive")
        {
            if (item.IsActive)
            {
                await OnArchiveCustomerAsync(item);
            }
            else
            {
                await OnRestoreCustomerAsync(item);
            }
        }
    }

    private async void OnEditCustomer(Customer customer)
    {
        using var dialog = new CustomerDialogForm(_contextFactory, _getCompanyId(), customer);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
    }

    private async Task OnArchiveCustomerAsync(CustomerRowViewModel item)
    {
        var confirm = MessageBox.Show(
            $"Archive client profile for '{item.Name}'?\n\nTheir records will be preserved, but hidden from active lists.",
            "Archive Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _customerService.ArchiveCustomerAsync(item.CustomerId);
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.GetBaseException().Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnRestoreCustomerAsync(CustomerRowViewModel item)
    {
        var confirm = MessageBox.Show(
            $"Restore and reactivate client '{item.Name}'?",
            "Restore Customer",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _customerService.RestoreCustomerAsync(item.CustomerId);
            await LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.GetBaseException().Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChkShowArchived_CheckedChanged(object? sender, EventArgs e)
    {
        bool showArchived = chkShowArchived?.Checked ?? false;
        UpdateActionColumnMode(showArchived);
        _ = LoadCustomerDataAsync(searchBar1?.TextValue.Trim() ?? "");
    }

    public async Task LoadCustomerDataAsync(string search = "")
    {
        if (DesignMode) return;

        try
        {
            int companyId = _getCompanyId();
            bool showArchived = chkShowArchived?.Checked ?? false;

            var displayList = await _customerService.GetCustomersAsync(companyId, showArchived, search);

            if (dgvCustomers != null)
            {
                dgvCustomers.DataSource = displayList;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customer profiles: {ex.GetBaseException().Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnNewCustomer_Click(object? sender, EventArgs e)
    {
        using var dialog = new CustomerDialogForm(_contextFactory, _getCompanyId());

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

    private void SearchBar1_Load(object? sender, EventArgs e)
    {
        if (searchBar1 != null)
        {
            searchBar1.SearchTextChanged += SearchBar1_SearchTextChanged;
        }
    }
}