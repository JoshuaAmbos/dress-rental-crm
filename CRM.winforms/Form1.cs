using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.winforms;

public partial class Form1 : Form
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private ComboBox cboTenant = null!;
    private TextBox txtCustomerCode = null!;
    private TextBox txtCustomerName = null!;
    private TextBox txtContactNumber = null!;
    private TextBox txtDressDescription = null!;
    private NumericUpDown numRentalFee = null!;
    private NumericUpDown numDeposit = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private CheckBox chkAgreed = null!;
    private Button btnSave = null!;
    private Button btnRefresh = null!;
    private DataGridView dgvBookings = null!;

    public Form1()
    {
        InitializeLowFiUI();
        LoadBookings();
    }

    private void InitializeLowFiUI()
    {
        this.Text = "Dress Rental CRM - Client Intake & Rental Transactions";
        this.Width = 960;
        this.Height = 640;
        this.StartPosition = FormStartPosition.CenterScreen;

        // GroupBox: 3 Tenants Selection
        var grpTenant = new GroupBox { Text = "Active Store Tenant (Multi-Tenant)", Left = 20, Top = 10, Width = 905, Height = 55 };
        var lblTenant = new Label { Text = "Select Tenant:", Left = 15, Top = 22, Width = 90 };
        cboTenant = new ComboBox { Left = 110, Top = 18, Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
        cboTenant.Items.AddRange(new object[] {
            "Tenant 1 - Atelier Manila Boutique (COMP001)",
            "Tenant 2 - Cebu Bridal & Gown Studio (COMP002)",
            "Tenant 3 - Davao Haute Rentals (COMP003)"
        });
        cboTenant.SelectedIndex = 0;
        cboTenant.SelectedIndexChanged += (s, e) => LoadBookings();
        grpTenant.Controls.AddRange(new Control[] { lblTenant, cboTenant });

        // GroupBox: Data Collection (User Inputs)
        var grpInputs = new GroupBox { Text = "Transaction Intake & Rental Form (UC-01 & UC-02)", Left = 20, Top = 70, Width = 905, Height = 175 };

        grpInputs.Controls.Add(new Label { Text = "Customer Code:", Left = 15, Top = 25, Width = 100 });
        txtCustomerCode = new TextBox { Left = 120, Top = 22, Width = 150, Text = "CUST-101" };

        grpInputs.Controls.Add(new Label { Text = "Customer Name:", Left = 290, Top = 25, Width = 100 });
        txtCustomerName = new TextBox { Left = 395, Top = 22, Width = 200, Text = "Maria Clara" };

        grpInputs.Controls.Add(new Label { Text = "Contact Number:", Left = 615, Top = 25, Width = 100 });
        txtContactNumber = new TextBox { Left = 720, Top = 22, Width = 165, Text = "09171234567" };

        grpInputs.Controls.Add(new Label { Text = "Dress / Style:", Left = 15, Top = 60, Width = 100 });
        txtDressDescription = new TextBox { Left = 120, Top = 57, Width = 475, Text = "Emerald Velvet Evening Gown" };

        grpInputs.Controls.Add(new Label { Text = "Rental Fee (PHP):", Left = 615, Top = 60, Width = 100 });
        numRentalFee = new NumericUpDown { Left = 720, Top = 57, Width = 165, Maximum = 100000, Value = 3500 };

        grpInputs.Controls.Add(new Label { Text = "Start Date:", Left = 15, Top = 95, Width = 100 });
        dtpStartDate = new DateTimePicker { Left = 120, Top = 92, Width = 150 };

        grpInputs.Controls.Add(new Label { Text = "End Date:", Left = 290, Top = 95, Width = 100 });
        dtpEndDate = new DateTimePicker { Left = 395, Top = 92, Width = 150, Value = DateTime.Now.AddDays(3) };

        grpInputs.Controls.Add(new Label { Text = "Deposit (PHP):", Left = 615, Top = 95, Width = 100 });
        numDeposit = new NumericUpDown { Left = 720, Top = 92, Width = 165, Maximum = 50000, Value = 1000 };

        chkAgreed = new CheckBox { Text = "Customer Agreed to Rental Terms & Deposit Policy", Left = 120, Top = 132, Width = 350, Checked = true };

        btnSave = new Button { Text = "Save Transaction", Left = 600, Top = 128, Width = 140, Height = 32 };
        btnSave.Click += BtnSave_Click;

        btnRefresh = new Button { Text = "Reload Data", Left = 750, Top = 128, Width = 135, Height = 32 };
        btnRefresh.Click += (s, e) => LoadBookings();

        grpInputs.Controls.AddRange(new Control[] {
            txtCustomerCode, txtCustomerName, txtContactNumber, txtDressDescription,
            numRentalFee, dtpStartDate, dtpEndDate, numDeposit, chkAgreed, btnSave, btnRefresh
        });

        // DataGridView: Data Display
        dgvBookings = new DataGridView
        {
            Left = 20,
            Top = 255,
            Width = 905,
            Height = 330,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        this.Controls.AddRange(new Control[] { grpTenant, grpInputs, dgvBookings });
    }

    private TenantCrmDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCustomerName.Text) || string.IsNullOrWhiteSpace(txtDressDescription.Text))
        {
            MessageBox.Show("Please fill in Customer Name and Dress Description.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!chkAgreed.Checked)
        {
            MessageBox.Show("Rental terms must be accepted before saving.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await using var db = GetDbContext();

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerCode == txtCustomerCode.Text.Trim());
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerCode = txtCustomerCode.Text.Trim(),
                    CustomerName = txtCustomerName.Text.Trim(),
                    ContactNumber = txtContactNumber.Text.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(customer);
                await db.SaveChangesAsync();
            }

            var booking = new RentalBooking
            {
                CustomerId = customer.CustomerId,
                DressDescription = txtDressDescription.Text.Trim(),
                RentalStartDate = dtpStartDate.Value.Date,
                RentalEndDate = dtpEndDate.Value.Date,
                RentalFee = numRentalFee.Value,
                SecurityDeposit = numDeposit.Value,
                BookingStage = "Reserved",
                AgreedToTerms = chkAgreed.Checked,
                CreatedAt = DateTime.UtcNow
            };

            db.RentalBookings.Add(booking);
            await db.SaveChangesAsync();

            MessageBox.Show("Rental Transaction Saved Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadBookings();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving transaction: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void LoadBookings()
    {
        try
        {
            await using var db = GetDbContext();
            var list = await db.RentalBookings
                .Include(b => b.Customer)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    BookingID = b.RentalBookingId,
                    Customer = b.Customer.CustomerName,
                    Dress = b.DressDescription,
                    b.RentalStartDate,
                    b.RentalEndDate,
                    Fee = b.RentalFee,
                    Deposit = b.SecurityDeposit,
                    Stage = b.BookingStage
                })
                .ToListAsync();

            dgvBookings.DataSource = list;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading records: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}