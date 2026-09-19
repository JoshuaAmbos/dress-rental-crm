using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.winforms.Forms;

public partial class CustomerDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _dbFactory;
    private readonly int _companyId;
    private readonly int? _customerId;

    private TextBox txtCode = null!;
    private TextBox txtFirstName = null!;
    private TextBox txtMiddleName = null!;
    private TextBox txtLastName = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtAddress = null!;
    private NumericUpDown numBust = null!;
    private NumericUpDown numWaist = null!;
    private NumericUpDown numHips = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    // Atelier Theme
    private static readonly Color ColorHeaderBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorBorder = Color.FromArgb(220, 210, 210);

    public CustomerDialogForm(Func<TenantCrmDbContext> dbFactory, int companyId, Customer? existingCustomer = null)
    {
        _dbFactory = dbFactory;
        _companyId = companyId;
        _customerId = existingCustomer?.CustomerId;

        InitializeUI();

        if (existingCustomer != null)
        {
            PopulateFields(existingCustomer);
            Text = "Edit Customer Profile";
        }
        else
        {
            Text = "New Customer Intake";
            GenerateDefaultCode();
        }
    }

    private void InitializeUI()
    {
        // Increased height from 540 to 640 to accommodate two new name fields
        ClientSize = new Size(460, 640);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = ColorHeaderBg
        };
        var lblTitle = new Label
        {
            Text = _customerId.HasValue ? "Update Customer Profile" : "Add New Customer",
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(24, 18),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);
        Controls.Add(pnlHeader);

        // Content Area
        int y = 80;
        txtCode = AddField("Customer Code *", ref y);
        txtFirstName = AddField("First Name *", ref y);
        txtMiddleName = AddField("Middle Name", ref y);
        txtLastName = AddField("Last Name *", ref y);
        txtPhone = AddField("Contact Number", ref y);
        txtEmail = AddField("Email Address", ref y);
        txtAddress = AddField("Address / City", ref y);

        // Measurements Row
        var lblMeasurements = new Label
        {
            Text = "Body Measurements (inches):",
            Location = new Point(24, y),
            AutoSize = true,
            ForeColor = ColorEspresso,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };
        Controls.Add(lblMeasurements);
        y += 24;

        numBust = AddMeasurementSpinner("Bust", 24, y, 34.0m);
        numWaist = AddMeasurementSpinner("Waist", 160, y, 26.0m);
        numHips = AddMeasurementSpinner("Hips", 296, y, 36.0m);
        y += 56;

        // Bottom Actions
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(253, 250, 250)
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(95, 34),
            Location = new Point(235, 13),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = ColorEspresso,
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ColorBorder;
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        btnSave = new Button
        {
            Text = "Save Profile",
            Size = new Size(110, 34),
            Location = new Point(336, 13),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorDustyRose,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += async (s, e) => await SaveCustomerAsync();

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(pnlFooter);
    }

    private TextBox AddField(string labelText, ref int y)
    {
        var lbl = new Label
        {
            Text = labelText,
            Location = new Point(24, y),
            AutoSize = true,
            ForeColor = ColorEspresso
        };
        var txt = new TextBox
        {
            Location = new Point(24, y + 20),
            Width = 412,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.AddRange(new Control[] { lbl, txt });
        y += 52;
        return txt;
    }

    private NumericUpDown AddMeasurementSpinner(string tag, int x, int y, decimal defaultVal)
    {
        var lbl = new Label
        {
            Text = tag,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 110, 115)
        };
        var num = new NumericUpDown
        {
            Location = new Point(x, y + 18),
            Width = 100,
            DecimalPlaces = 1,
            Minimum = 0,
            Maximum = 120,
            Value = defaultVal,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.AddRange(new Control[] { lbl, num });
        return num;
    }

    private void PopulateFields(Customer c)
    {
        txtCode.Text = c.CustomerCode;
        txtFirstName.Text = c.FirstName;
        txtMiddleName.Text = c.MiddleName;
        txtLastName.Text = c.LastName;
        txtPhone.Text = c.ContactNumber;
        txtEmail.Text = c.EmailAddress;
        txtAddress.Text = c.Address;

        numBust.Value = Math.Clamp(c.BustSize, numBust.Minimum, numBust.Maximum);
        numWaist.Value = Math.Clamp(c.WaistSize, numWaist.Minimum, numWaist.Maximum);
        numHips.Value = Math.Clamp(c.HipSize, numHips.Minimum, numHips.Maximum);
    }

    private void GenerateDefaultCode()
    {
        txtCode.Text = $"CUST-{DateTime.UtcNow:MMdd}-{Random.Shared.Next(100, 999)}";
    }

    private async Task SaveCustomerAsync()
    {
        var code = txtCode.Text.Trim();
        var fName = txtFirstName.Text.Trim();
        var lName = txtLastName.Text.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(fName) || string.IsNullOrWhiteSpace(lName))
        {
            MessageBox.Show("Customer Code, First Name, and Last Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSave.Enabled = false;

        try
        {
            await using var db = _dbFactory();

            var codeExists = await db.Customers.AnyAsync(c =>
                c.CompanyId == _companyId &&
                c.CustomerCode == code &&
                (!_customerId.HasValue || c.CustomerId != _customerId.Value));

            if (codeExists)
            {
                MessageBox.Show($"Customer code '{code}' is already assigned.", "Duplicate Code", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnSave.Enabled = true;
                return;
            }

            if (!_customerId.HasValue)
            {
                var newCustomer = new Customer
                {
                    CompanyId = _companyId,
                    CustomerCode = code,
                    FirstName = fName,
                    MiddleName = txtMiddleName.Text.Trim(),
                    LastName = lName,
                    ContactNumber = txtPhone.Text.Trim(),
                    EmailAddress = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    BustSize = numBust.Value,
                    WaistSize = numWaist.Value,
                    HipSize = numHips.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(newCustomer);
            }
            else
            {
                var existing = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == _customerId.Value);
                if (existing == null)
                {
                    MessageBox.Show("The customer record could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                existing.CustomerCode = code;
                existing.FirstName = fName;
                existing.MiddleName = txtMiddleName.Text.Trim();
                existing.LastName = lName;
                existing.ContactNumber = txtPhone.Text.Trim();
                existing.EmailAddress = txtEmail.Text.Trim();
                existing.Address = txtAddress.Text.Trim();
                existing.BustSize = numBust.Value;
                existing.WaistSize = numWaist.Value;
                existing.HipSize = numHips.Value;
            }

            await db.SaveChangesAsync();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save record: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnSave.Enabled = true;
        }
    }
}