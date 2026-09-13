using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.winforms;

public partial class Form1 : Form
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private static readonly string[] BookingStages =
    {
        "Reserved", "Fitting Scheduled", "Out for Rental", "Returned", "Cancelled"
    };

    //  Shared / Tenant 
    private ComboBox cboTenant = null!;
    private TabControl tabMain = null!;

    //  Clients Tab 
    private TextBox txtCustomerCode = null!;
    private TextBox txtCustomerName = null!;
    private TextBox txtContactNumber = null!;
    private TextBox txtEmailAddress = null!;
    private TextBox txtAddress = null!;
    private NumericUpDown numBust = null!;
    private NumericUpDown numWaist = null!;
    private NumericUpDown numHips = null!;
    private TextBox txtClientSearch = null!;
    private Button btnClientNew = null!;
    private Button btnClientSave = null!;
    private Button btnClientDelete = null!;
    private Button btnClientRefresh = null!;
    private DataGridView dgvClients = null!;
    private int? _selectedCustomerId;

    //  Rentals Tab 
    private ComboBox cboBookingCustomer = null!;
    private ComboBox cboBookingStage = null!;
    private TextBox txtBookingTenant = null!;
    private TextBox txtDressDescription = null!;
    private TextBox txtAlterationNotes = null!;
    private NumericUpDown numRentalFee = null!;
    private NumericUpDown numDeposit = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private CheckBox chkAgreed = null!;
    private TextBox txtBookingSearch = null!;
    private Button btnBookingNew = null!;
    private Button btnBookingSave = null!;
    private Button btnBookingDelete = null!;
    private Button btnBookingRefresh = null!;
    private DataGridView dgvBookings = null!;
    private int? _selectedBookingId;

    public Form1()
    {
        InitializeCrmUI();
        LoadClients();
        LoadBookings();
    }

    // 
    //  UI CONSTRUCTION
    // 

    private void InitializeCrmUI()
    {
        this.Text = "Dress Rental CRM - Client & Rental Management";
        this.Width = 1040;
        this.Height = 750;
        this.MinimumSize = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;

        var grpTenant = new GroupBox { Text = "Boutique Tenant", Left = 15, Top = 10, Width = 995, Height = 55 };
        var lblTenant = new Label { Text = "Active Tenant:", Left = 15, Top = 24, Width = 90 };
        cboTenant = new ComboBox { Left = 110, Top = 20, Width = 420, DropDownStyle = ComboBoxStyle.DropDownList };
        cboTenant.Items.AddRange(new object[]
        {
            "Tenant 1 - Atelier Manila Boutique (COMP001)",
            "Tenant 2 - Cebu Bridal & Gown Studio (COMP002)",
            "Tenant 3 - Davao Haute Rentals (COMP003)"
        });
        cboTenant.SelectedIndex = 0;
        cboTenant.SelectedIndexChanged += (s, e) =>
        {
            if (txtBookingTenant != null)
                txtBookingTenant.Text = cboTenant.SelectedItem?.ToString() ?? "";
            ClearClientForm();
            ClearBookingForm();
            LoadClients();
            LoadBookings();
        };
        grpTenant.Controls.AddRange(new Control[] { lblTenant, cboTenant });

        tabMain = new TabControl { Left = 15, Top = 75, Width = 995, Height = 620 };
        var tabClients = new TabPage("Clients && Measurements");
        var tabBookings = new TabPage("Rental Bookings");
        tabMain.TabPages.Add(tabClients);
        tabMain.TabPages.Add(tabBookings);

        BuildClientsTab(tabClients);
        BuildBookingsTab(tabBookings);

        this.Controls.Add(tabMain);
        this.Controls.Add(grpTenant);
    }

    private static void StyleActionButton(Button btn, string text, Color backColor, Color foreColor)
    {
        btn.Text = text;
        btn.BackColor = backColor;
        btn.ForeColor = foreColor;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
    }

    private void BuildClientsTab(TabPage page)
    {
        var grpForm = new GroupBox { Text = "Client Profile & Sizing (fields marked * are required)", Left = 10, Top = 10, Width = 965, Height = 175 };

        // Row 1: Code, Name, Contact, Email
        grpForm.Controls.Add(new Label { Text = "Client Code*:", Left = 15, Top = 25, Width = 85 });
        txtCustomerCode = new TextBox { Left = 105, Top = 22, Width = 110 };

        grpForm.Controls.Add(new Label { Text = "Client Name*:", Left = 225, Top = 25, Width = 85 });
        txtCustomerName = new TextBox { Left = 315, Top = 22, Width = 180 };

        grpForm.Controls.Add(new Label { Text = "Contact No*:", Left = 505, Top = 25, Width = 80 });
        txtContactNumber = new TextBox { Left = 590, Top = 22, Width = 130 };

        grpForm.Controls.Add(new Label { Text = "Email:", Left = 730, Top = 25, Width = 45 });
        txtEmailAddress = new TextBox { Left = 780, Top = 22, Width = 170 };

        // Row 2: Address, Bust, Waist, Hips
        grpForm.Controls.Add(new Label { Text = "Address:", Left = 15, Top = 62, Width = 85 });
        txtAddress = new TextBox { Left = 105, Top = 59, Width = 390 };

        grpForm.Controls.Add(new Label { Text = "Bust (in)*:", Left = 505, Top = 62, Width = 65 });
        numBust = new NumericUpDown { Left = 575, Top = 59, Width = 60, DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = 34.5m };

        grpForm.Controls.Add(new Label { Text = "Waist (in)*:", Left = 645, Top = 62, Width = 70 });
        numWaist = new NumericUpDown { Left = 720, Top = 59, Width = 60, DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = 26.0m };

        grpForm.Controls.Add(new Label { Text = "Hips (in)*:", Left = 790, Top = 62, Width = 65 });
        numHips = new NumericUpDown { Left = 860, Top = 59, Width = 60, DecimalPlaces = 1, Minimum = 0, Maximum = 100, Value = 36.0m };

        // Row 3: Action Buttons
        btnClientNew = new Button { Left = 590, Top = 105, Width = 105, Height = 40 };
        StyleActionButton(btnClientNew, "New Client", Color.FromArgb(70, 80, 95), Color.White);
        btnClientNew.Click += (s, e) => ClearClientForm();

        btnClientSave = new Button { Left = 705, Top = 105, Width = 125, Height = 40 };
        StyleActionButton(btnClientSave, "Save Client", Color.FromArgb(114, 47, 55), Color.White);
        btnClientSave.Click += BtnClientSave_Click;

        btnClientDelete = new Button { Left = 840, Top = 105, Width = 110, Height = 40 };
        StyleActionButton(btnClientDelete, "Delete", Color.FromArgb(192, 57, 43), Color.White);
        btnClientDelete.Click += BtnClientDelete_Click;

        var lblHint = new Label { Text = "Tip: Click a client row below to load their measurements and profile for editing.", Left = 15, Top = 118, Width = 550, ForeColor = Color.DimGray };

        grpForm.Controls.AddRange(new Control[]
        {
            txtCustomerCode, txtCustomerName, txtContactNumber, txtEmailAddress,
            txtAddress, numBust, numWaist, numHips,
            btnClientNew, btnClientSave, btnClientDelete, lblHint
        });

        var lblSearch = new Label { Text = "Search:", Left = 10, Top = 197, Width = 55 };
        txtClientSearch = new TextBox { Left = 70, Top = 194, Width = 280 };
        txtClientSearch.TextChanged += (s, e) => LoadClients(txtClientSearch.Text.Trim());

        btnClientRefresh = new Button { Text = "Refresh", Left = 360, Top = 192, Width = 80, Height = 28 };
        btnClientRefresh.Click += (s, e) => { txtClientSearch.Text = ""; LoadClients(); };

        dgvClients = new DataGridView
        {
            Left = 10,
            Top = 230,
            Width = 965,
            Height = 345,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false
        };
        dgvClients.SelectionChanged += DgvClients_SelectionChanged;
        dgvClients.DataBindingComplete += (s, e) => HideHelperColumns(dgvClients, "CustomerId", "CompanyId");

        page.Controls.AddRange(new Control[] { grpForm, lblSearch, txtClientSearch, btnClientRefresh, dgvClients });
    }

    private void BuildBookingsTab(TabPage page)
    {
        var grpForm = new GroupBox { Text = "Rental Consultation & Fitting (fields marked * are required)", Left = 10, Top = 10, Width = 965, Height = 225 };

        grpForm.Controls.Add(new Label { Text = "Client*:", Left = 15, Top = 25, Width = 50 });
        cboBookingCustomer = new ComboBox { Left = 70, Top = 22, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

        grpForm.Controls.Add(new Label { Text = "Stage:", Left = 365, Top = 25, Width = 45 });
        cboBookingStage = new ComboBox { Left = 415, Top = 22, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
        cboBookingStage.Items.AddRange(BookingStages);
        cboBookingStage.SelectedIndex = 0;

        grpForm.Controls.Add(new Label { Text = "Tenant:", Left = 570, Top = 25, Width = 55 });
        txtBookingTenant = new TextBox { Left = 630, Top = 22, Width = 320, ReadOnly = true, Text = cboTenant.SelectedItem?.ToString() ?? "" };

        grpForm.Controls.Add(new Label { Text = "Gown*:", Left = 15, Top = 62, Width = 50 });
        txtDressDescription = new TextBox { Left = 70, Top = 59, Width = 880 };

        grpForm.Controls.Add(new Label { Text = "Alterations:", Left = 15, Top = 99, Width = 70 });
        txtAlterationNotes = new TextBox { Left = 90, Top = 96, Width = 860 };

        grpForm.Controls.Add(new Label { Text = "Fee (PHP)*:", Left = 15, Top = 136, Width = 75 });
        numRentalFee = new NumericUpDown { Left = 95, Top = 133, Width = 90, Maximum = 200000, Value = 3500 };

        grpForm.Controls.Add(new Label { Text = "Deposit (PHP)*:", Left = 200, Top = 136, Width = 95 });
        numDeposit = new NumericUpDown { Left = 300, Top = 133, Width = 90, Maximum = 100000, Value = 1000 };

        grpForm.Controls.Add(new Label { Text = "Pickup*:", Left = 410, Top = 136, Width = 55 });
        dtpStartDate = new DateTimePicker { Left = 470, Top = 133, Width = 120, Format = DateTimePickerFormat.Short };

        grpForm.Controls.Add(new Label { Text = "Return*:", Left = 610, Top = 136, Width = 55 });
        dtpEndDate = new DateTimePicker { Left = 670, Top = 133, Width = 120, Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(3) };

        chkAgreed = new CheckBox { Text = "Terms & Deposit Policy Agreed*", Left = 15, Top = 178, Width = 250, Checked = true };

        btnBookingNew = new Button { Left = 570, Top = 170, Width = 110, Height = 40 };
        StyleActionButton(btnBookingNew, "New Booking", Color.FromArgb(70, 80, 95), Color.White);
        btnBookingNew.Click += (s, e) => ClearBookingForm();

        btnBookingSave = new Button { Left = 690, Top = 170, Width = 145, Height = 40 };
        StyleActionButton(btnBookingSave, "Save Booking", Color.FromArgb(114, 47, 55), Color.White);
        btnBookingSave.Click += BtnBookingSave_Click;

        btnBookingDelete = new Button { Left = 845, Top = 170, Width = 105, Height = 40 };
        StyleActionButton(btnBookingDelete, "Delete", Color.FromArgb(192, 57, 43), Color.White);
        btnBookingDelete.Click += BtnBookingDelete_Click;

        grpForm.Controls.AddRange(new Control[]
        {
            cboBookingCustomer, cboBookingStage, txtBookingTenant, txtDressDescription, txtAlterationNotes,
            numRentalFee, numDeposit, dtpStartDate, dtpEndDate, chkAgreed,
            btnBookingNew, btnBookingSave, btnBookingDelete
        });

        var lblSearch = new Label { Text = "Search:", Left = 10, Top = 247, Width = 55 };
        txtBookingSearch = new TextBox { Left = 70, Top = 244, Width = 280 };
        txtBookingSearch.TextChanged += (s, e) => LoadBookings(txtBookingSearch.Text.Trim());

        btnBookingRefresh = new Button { Text = "Refresh", Left = 360, Top = 242, Width = 80, Height = 28 };
        btnBookingRefresh.Click += (s, e) => { txtBookingSearch.Text = ""; LoadBookings(); };

        var lblLegend = new Label
        {
            Text = "Rows shaded pink are overdue (past return date, not yet returned/cancelled).",
            Left = 460,
            Top = 247,
            Width = 490,
            ForeColor = Color.DimGray
        };

        dgvBookings = new DataGridView
        {
            Left = 10,
            Top = 280,
            Width = 965,
            Height = 295,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false
        };
        dgvBookings.SelectionChanged += DgvBookings_SelectionChanged;
        dgvBookings.DataBindingComplete += DgvBookings_DataBindingComplete;

        page.Controls.AddRange(new Control[] { grpForm, lblSearch, txtBookingSearch, btnBookingRefresh, lblLegend, dgvBookings });
    }

    // 
    //  HELPERS
    // 

    private int GetActiveCompanyId() => cboTenant.SelectedIndex + 1;

    private TenantCrmDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }

    private static void HideHelperColumns(DataGridView grid, params string[] columnNames)
    {
        foreach (var name in columnNames)
        {
            if (grid.Columns.Contains(name))
                grid.Columns[name]!.Visible = false;
        }
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        var cleaned = Regex.Replace(phone, @"[\s\-()]", "");
        return Regex.IsMatch(cleaned, @"^\+?\d{7,15}$");
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private sealed class CustomerListItem
    {
        public int Id { get; init; }
        public string Display { get; init; } = "";
        public override string ToString() => Display;
    }

    // 
    //  CLIENTS: LOAD / FILTER (ACTIVE TENANT ONLY)
    // 

    private async void LoadClients(string search = "")
    {
        try
        {
            await using var db = GetDbContext();
            var activeCompanyId = GetActiveCompanyId();

            // Filter clients exclusively to the active tenant boutique
            var query = db.Customers.Where(c => c.CompanyId == activeCompanyId).AsQueryable();

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
                .Select(c => new
                {
                    c.CustomerId,
                    c.CompanyId,
                    Code = c.CustomerCode,
                    Name = c.CustomerName,
                    Phone = c.ContactNumber,
                    Email = c.EmailAddress,
                    Address = c.Address,
                    Bust = c.BustSize,
                    Waist = c.WaistSize,
                    Hips = c.HipSize,
                    Registered = c.CreatedAt.ToShortDateString()
                })
                .ToListAsync();

            dgvClients.DataSource = list;
            await RefreshCustomerComboBoxAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading clients: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async System.Threading.Tasks.Task RefreshCustomerComboBoxAsync()
    {
        try
        {
            await using var db = GetDbContext();
            var activeCompanyId = GetActiveCompanyId();

            // Only populate clients registered to the active boutique
            var customers = await db.Customers
                .Where(c => c.CompanyId == activeCompanyId)
                .OrderBy(c => c.CustomerName)
                .Select(c => new CustomerListItem { Id = c.CustomerId, Display = $"{c.CustomerCode} - {c.CustomerName}" })
                .ToListAsync();

            var previouslySelected = (cboBookingCustomer.SelectedItem as CustomerListItem)?.Id;

            cboBookingCustomer.Items.Clear();
            foreach (var c in customers)
                cboBookingCustomer.Items.Add(c);

            if (previouslySelected.HasValue)
            {
                var match = customers.FirstOrDefault(c => c.Id == previouslySelected.Value);
                if (match != null) cboBookingCustomer.SelectedItem = match;
            }
        }
        catch
        {
            // Non-fatal
        }
    }

    private void DgvClients_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvClients.CurrentRow == null) return;

        var row = dgvClients.CurrentRow;
        _selectedCustomerId = Convert.ToInt32(row.Cells["CustomerId"].Value);
        txtCustomerCode.Text = row.Cells["Code"].Value?.ToString() ?? "";
        txtCustomerName.Text = row.Cells["Name"].Value?.ToString() ?? "";
        txtContactNumber.Text = row.Cells["Phone"].Value?.ToString() ?? "";
        txtEmailAddress.Text = row.Cells["Email"].Value?.ToString() ?? "";
        txtAddress.Text = row.Cells["Address"].Value?.ToString() ?? "";

        var bust = Convert.ToDecimal(row.Cells["Bust"].Value ?? 0);
        var waist = Convert.ToDecimal(row.Cells["Waist"].Value ?? 0);
        var hips = Convert.ToDecimal(row.Cells["Hips"].Value ?? 0);

        numBust.Value = Math.Clamp(bust, numBust.Minimum, numBust.Maximum);
        numWaist.Value = Math.Clamp(waist, numWaist.Minimum, numWaist.Maximum);
        numHips.Value = Math.Clamp(hips, numHips.Minimum, numHips.Maximum);
    }

    private void ClearClientForm()
    {
        _selectedCustomerId = null;
        txtCustomerCode.Clear();
        txtCustomerName.Clear();
        txtContactNumber.Clear();
        txtEmailAddress.Clear();
        txtAddress.Clear();
        numBust.Value = 34.5m;
        numWaist.Value = 26.0m;
        numHips.Value = 36.0m;
        dgvClients.ClearSelection();
        txtCustomerCode.Focus();
    }

    // =========================================================
    //  CLIENTS: VALIDATE / SAVE / DELETE
    // =========================================================

    private string? ValidateClientForm()
    {
        if (string.IsNullOrWhiteSpace(txtCustomerCode.Text))
            return "Client Code is required.";
        if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            return "Client Name is required.";
        if (string.IsNullOrWhiteSpace(txtContactNumber.Text))
            return "Contact Number is required.";
        if (!IsValidPhoneNumber(txtContactNumber.Text))
            return "Contact Number looks invalid. Use digits only, 7-15 digits (optionally starting with +).";
        if (!string.IsNullOrWhiteSpace(txtEmailAddress.Text) && !IsValidEmail(txtEmailAddress.Text.Trim()))
            return "Email Address format is invalid.";
        if (numBust.Value <= 0 || numWaist.Value <= 0 || numHips.Value <= 0)
            return "Bust, Waist, and Hips measurements must be greater than zero.";

        return null;
    }

    private async void BtnClientSave_Click(object? sender, EventArgs e)
    {
        var error = ValidateClientForm();
        if (error != null)
        {
            MessageBox.Show(error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await using var db = GetDbContext();
            var code = txtCustomerCode.Text.Trim();
            var activeCompanyId = GetActiveCompanyId();

            var duplicate = await db.Customers.FirstOrDefaultAsync(c =>
                c.CompanyId == activeCompanyId &&
                c.CustomerCode == code &&
                (_selectedCustomerId == null || c.CustomerId != _selectedCustomerId));

            if (duplicate != null)
            {
                MessageBox.Show("Another client in this boutique already uses this Client Code. Please use a unique code.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedCustomerId == null)
            {
                var customer = new Customer
                {
                    CompanyId = activeCompanyId, // Scopes client to active boutique tenant
                    CustomerCode = code,
                    CustomerName = txtCustomerName.Text.Trim(),
                    ContactNumber = txtContactNumber.Text.Trim(),
                    EmailAddress = txtEmailAddress.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    BustSize = numBust.Value,
                    WaistSize = numWaist.Value,
                    HipSize = numHips.Value,
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
                MessageBox.Show("Client added successfully.", "CRM Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == _selectedCustomerId);
                if (customer == null)
                {
                    MessageBox.Show("This client no longer exists. It may have been deleted elsewhere.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ClearClientForm();
                    LoadClients();
                    return;
                }

                customer.CustomerCode = code;
                customer.CustomerName = txtCustomerName.Text.Trim();
                customer.ContactNumber = txtContactNumber.Text.Trim();
                customer.EmailAddress = txtEmailAddress.Text.Trim();
                customer.Address = txtAddress.Text.Trim();
                customer.BustSize = numBust.Value;
                customer.WaistSize = numWaist.Value;
                customer.HipSize = numHips.Value;
                await db.SaveChangesAsync();
                MessageBox.Show("Client updated successfully.", "CRM Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ClearClientForm();
            LoadClients();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnClientDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedCustomerId == null)
        {
            MessageBox.Show("Select a client from the list first.", "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            await using var db = GetDbContext();
            var hasBookings = await db.RentalBookings.AnyAsync(b => b.CustomerId == _selectedCustomerId);
            if (hasBookings)
            {
                MessageBox.Show(
                    "This client has rental booking records and cannot be deleted. Delete or reassign their bookings first.",
                    "Cannot Delete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete client '{txtCustomerName.Text}'? This cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == _selectedCustomerId);
            if (customer != null)
            {
                db.Customers.Remove(customer);
                await db.SaveChangesAsync();
            }

            ClearClientForm();
            LoadClients();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // 
    //  BOOKINGS: LOAD / FILTER (ACTIVE TENANT ONLY)
    // 

    private async void LoadBookings(string search = "")
    {
        try
        {
            await using var db = GetDbContext();
            var activeCompanyId = GetActiveCompanyId();

            // Filter bookings exclusively to the active tenant boutique
            var query = db.RentalBookings
                .Include(b => b.Customer)
                .Where(b => b.CompanyId == activeCompanyId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.Customer.CustomerName.Contains(search) ||
                    b.DressDescription.Contains(search) ||
                    b.BookingStage.Contains(search) ||
                    (b.AlterationNotes != null && b.AlterationNotes.Contains(search)));
            }

            var today = DateTime.Today;

            var list = await query
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.RentalBookingId,
                    b.CustomerId,
                    b.CompanyId,
                    Tenant = b.CompanyId == 2 ? "Tenant 2 - Cebu Bridal & Gown Studio (COMP002)" :
                             b.CompanyId == 3 ? "Tenant 3 - Davao Haute Rentals (COMP003)" :
                             "Tenant 1 - Atelier Manila Boutique (COMP001)",
                    Client = b.Customer.CustomerName,
                    Phone = b.Customer.ContactNumber,
                    Gown = b.DressDescription,
                    Alterations = b.AlterationNotes ?? "",
                    Stage = b.BookingStage,
                    Pickup = b.RentalStartDate.ToShortDateString(),
                    Return = b.RentalEndDate.ToShortDateString(),
                    Fee = b.RentalFee,
                    Deposit = b.SecurityDeposit,
                    IsOverdue = b.RentalEndDate.Date < today && b.BookingStage != "Returned" && b.BookingStage != "Cancelled"
                })
                .ToListAsync();

            dgvBookings.DataSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading bookings: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DgvBookings_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
    {
        HideHelperColumns(dgvBookings, "RentalBookingId", "CustomerId", "CompanyId", "IsOverdue");

        foreach (DataGridViewRow row in dgvBookings.Rows)
        {
            var overdueCell = row.Cells["IsOverdue"];
            var isOverdue = overdueCell?.Value is bool b && b;
            row.DefaultCellStyle.BackColor = isOverdue ? Color.MistyRose : Color.White;
        }
    }

    private void DgvBookings_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvBookings.CurrentRow == null) return;

        var row = dgvBookings.CurrentRow;
        _selectedBookingId = Convert.ToInt32(row.Cells["RentalBookingId"].Value);
        var customerId = Convert.ToInt32(row.Cells["CustomerId"].Value);

        foreach (var item in cboBookingCustomer.Items)
        {
            if (item is CustomerListItem cli && cli.Id == customerId)
            {
                cboBookingCustomer.SelectedItem = cli;
                break;
            }
        }

        var tenantVal = row.Cells["Tenant"].Value?.ToString();
        txtBookingTenant.Text = string.IsNullOrWhiteSpace(tenantVal)
            ? (cboTenant.SelectedItem?.ToString() ?? "")
            : tenantVal;

        txtDressDescription.Text = row.Cells["Gown"].Value?.ToString() ?? "";
        txtAlterationNotes.Text = row.Cells["Alterations"].Value?.ToString() ?? "";
        var stage = row.Cells["Stage"].Value?.ToString() ?? BookingStages[0];
        cboBookingStage.SelectedItem = cboBookingStage.Items.Contains(stage) ? stage : BookingStages[0];

        if (DateTime.TryParse(row.Cells["Pickup"].Value?.ToString(), out var pickup))
            dtpStartDate.Value = pickup;
        if (DateTime.TryParse(row.Cells["Return"].Value?.ToString(), out var ret))
            dtpEndDate.Value = ret;

        numRentalFee.Value = Convert.ToDecimal(row.Cells["Fee"].Value);
        numDeposit.Value = Convert.ToDecimal(row.Cells["Deposit"].Value);

        chkAgreed.Checked = true;
    }

    private void ClearBookingForm()
    {
        _selectedBookingId = null;
        cboBookingCustomer.SelectedIndex = cboBookingCustomer.Items.Count > 0 ? 0 : -1;
        cboBookingStage.SelectedIndex = 0;
        txtBookingTenant.Text = cboTenant.SelectedItem?.ToString() ?? "";
        txtDressDescription.Clear();
        txtAlterationNotes.Clear();
        numRentalFee.Value = 3500;
        numDeposit.Value = 1000;
        dtpStartDate.Value = DateTime.Now;
        dtpEndDate.Value = DateTime.Now.AddDays(3);
        chkAgreed.Checked = true;
        dgvBookings.ClearSelection();
    }

    // =========================================================
    //  BOOKINGS: VALIDATE / SAVE / DELETE
    // =========================================================

    private string? ValidateBookingForm()
    {
        if (cboBookingCustomer.SelectedItem is not CustomerListItem)
            return "Please select a client for this booking.";
        if (string.IsNullOrWhiteSpace(txtDressDescription.Text))
            return "Gown description is required.";
        if (numRentalFee.Value <= 0)
            return "Rental Fee must be greater than zero.";
        if (numDeposit.Value < 0)
            return "Security Deposit cannot be negative.";
        if (dtpEndDate.Value.Date <= dtpStartDate.Value.Date)
            return "Return Date must be after the Pickup Date.";
        if (!chkAgreed.Checked)
            return "Client must acknowledge the rental terms and deposit policy.";

        return null;
    }

    private async void BtnBookingSave_Click(object? sender, EventArgs e)
    {
        var error = ValidateBookingForm();
        if (error != null)
        {
            MessageBox.Show(error, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedCustomer = (CustomerListItem)cboBookingCustomer.SelectedItem!;
        var activeCompanyId = GetActiveCompanyId();

        try
        {
            await using var db = GetDbContext();

            if (_selectedBookingId == null)
            {
                var booking = new RentalBooking
                {
                    CustomerId = selectedCustomer.Id,
                    CompanyId = activeCompanyId,
                    DressDescription = txtDressDescription.Text.Trim(),
                    AlterationNotes = txtAlterationNotes.Text.Trim(),
                    RentalStartDate = dtpStartDate.Value.Date,
                    RentalEndDate = dtpEndDate.Value.Date,
                    RentalFee = numRentalFee.Value,
                    SecurityDeposit = numDeposit.Value,
                    BookingStage = cboBookingStage.SelectedItem?.ToString() ?? BookingStages[0],
                    AgreedToTerms = chkAgreed.Checked,
                    CreatedAt = DateTime.UtcNow
                };

                db.RentalBookings.Add(booking);
                await db.SaveChangesAsync();
                MessageBox.Show("Rental booking recorded successfully.", "CRM Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var booking = await db.RentalBookings.FirstOrDefaultAsync(b => b.RentalBookingId == _selectedBookingId);
                if (booking == null)
                {
                    MessageBox.Show("This booking no longer exists. It may have been deleted elsewhere.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ClearBookingForm();
                    LoadBookings();
                    return;
                }

                booking.CustomerId = selectedCustomer.Id;
                booking.CompanyId = activeCompanyId;
                booking.DressDescription = txtDressDescription.Text.Trim();
                booking.AlterationNotes = txtAlterationNotes.Text.Trim();
                booking.RentalStartDate = dtpStartDate.Value.Date;
                booking.RentalEndDate = dtpEndDate.Value.Date;
                booking.RentalFee = numRentalFee.Value;
                booking.SecurityDeposit = numDeposit.Value;
                booking.BookingStage = cboBookingStage.SelectedItem?.ToString() ?? BookingStages[0];
                booking.AgreedToTerms = chkAgreed.Checked;

                await db.SaveChangesAsync();
                MessageBox.Show("Rental booking updated successfully.", "CRM Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            ClearBookingForm();
            LoadBookings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnBookingDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedBookingId == null)
        {
            MessageBox.Show("Select a booking from the list first.", "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Delete this rental booking? This cannot be undone.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            await using var db = GetDbContext();
            var booking = await db.RentalBookings.FirstOrDefaultAsync(b => b.RentalBookingId == _selectedBookingId);
            if (booking != null)
            {
                db.RentalBookings.Remove(booking);
                await db.SaveChangesAsync();
            }

            ClearBookingForm();
            LoadBookings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Database error: {ex.Message}", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}