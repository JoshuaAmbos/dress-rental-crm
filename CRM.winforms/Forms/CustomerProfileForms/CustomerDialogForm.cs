using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;

namespace CRM.winforms.Forms;

public partial class CustomerDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _dbFactory;
    private readonly int _companyId;
    private readonly int? _customerId;
    private readonly string _existingCustomerCode = string.Empty;

    public int? CreatedCustomerId { get; private set; }

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
    private Label lblHeaderSubtitle = null!;

    private static readonly Color ColorHeaderBg = Color.FromArgb(250, 245, 245);
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorMutedText = Color.FromArgb(120, 110, 115);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorDustyRoseHover = Color.FromArgb(171, 99, 108);
    private static readonly Color ColorBorder = Color.FromArgb(224, 216, 216);
    private static readonly Color ColorCardBg = Color.FromArgb(254, 252, 252);

    public CustomerDialogForm(Func<TenantCrmDbContext> dbFactory, int companyId)
        : this(dbFactory, companyId, null)
    {
    }

    public CustomerDialogForm(Func<TenantCrmDbContext> dbFactory, int companyId, Customer? existingCustomer)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _companyId = companyId;
        _customerId = existingCustomer?.CustomerId;

        InitializeUI();

        if (existingCustomer != null)
        {
            _existingCustomerCode = existingCustomer.CustomerCode;
            PopulateFields(existingCustomer);
            Text = "Edit Customer Profile";
            lblHeaderSubtitle.Text = $"Customer Code: {_existingCustomerCode}";
        }
        else
        {
            Text = "New Customer Intake";
        }
    }

    private void InitializeUI()
    {
        ClientSize = new Size(640, 690);
        MinimumSize = new Size(640, 600);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 75,
            BackColor = ColorHeaderBg,
            Padding = new Padding(28, 14, 28, 14)
        };

        var lblTitle = new Label
        {
            Text = _customerId.HasValue ? "Edit Customer Profile" : "Register New Customer",
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(28, 14),
            AutoSize = true
        };

        lblHeaderSubtitle = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = ColorMutedText,
            Location = new Point(29, 43),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblHeaderSubtitle });

        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = ColorHeaderBg,
            Padding = new Padding(28, 14, 28, 14)
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(100, 36),
            Location = new Point(390, 14),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = ColorEspresso,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ColorBorder;
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        btnSave = new Button
        {
            Text = _customerId.HasValue ? "Update Client" : "Save Profile",
            Size = new Size(114, 36),
            Location = new Point(500, 14),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorDustyRose,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = ColorDustyRoseHover;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = ColorDustyRose;
        btnSave.Click += async (s, e) => await SaveCustomerAsync();

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });

        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(28, 15, 28, 15),
            BackColor = Color.White
        };

        int currentY = 15;
        AddSectionHeader("Personal & Contact Details", ref currentY, pnlBody);

        txtFirstName = AddCompactField("First Name *", 28, currentY, 275, pnlBody);
        txtMiddleName = AddCompactField("Middle Name (Optional)", 323, currentY, 275, pnlBody);
        currentY += 58;

        txtLastName = AddCompactField("Last Name *", 28, currentY, 275, pnlBody);
        txtPhone = AddCompactField("Contact Number *", 323, currentY, 275, pnlBody);
        currentY += 58;

        txtEmail = AddCompactField("Email Address", 28, currentY, 570, pnlBody);
        currentY += 58;

        txtAddress = AddCompactField("Address / City", 28, currentY, 570, pnlBody);
        currentY += 68;

        AddSectionHeader("Body Measurements (Inches)", ref currentY, pnlBody);

        var pnlMeasurements = new Panel
        {
            Location = new Point(28, currentY),
            Size = new Size(570, 84),
            BackColor = ColorCardBg,
            BorderStyle = BorderStyle.FixedSingle
        };

        numBust = AddMeasurementSpinner("Bust", 25, 12, 34.0m, pnlMeasurements);
        numWaist = AddMeasurementSpinner("Waist", 215, 12, 26.0m, pnlMeasurements);
        numHips = AddMeasurementSpinner("Hips", 405, 12, 36.0m, pnlMeasurements);

        pnlBody.Controls.Add(pnlMeasurements);

        Controls.Add(pnlBody);
        Controls.Add(pnlHeader);
        Controls.Add(pnlFooter);

        pnlBody.BringToFront();
    }

    private void AddSectionHeader(string title, ref int y, Control parent)
    {
        var lbl = new Label
        {
            Text = title.ToUpperInvariant(),
            Location = new Point(28, y),
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = ColorDustyRose
        };
        parent.Controls.Add(lbl);
        y += 24;
    }

    private TextBox AddCompactField(string labelText, int x, int y, int width, Control parent)
    {
        var lbl = new Label
        {
            Text = labelText,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = ColorEspresso,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular)
        };

        var txt = new TextBox
        {
            Location = new Point(x, y + 20),
            Width = width,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };

        parent.Controls.AddRange(new Control[] { lbl, txt });
        return txt;
    }

    private NumericUpDown AddMeasurementSpinner(string tag, int x, int y, decimal defaultVal, Control parent)
    {
        var lbl = new Label
        {
            Text = $"{tag} (in)",
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = ColorEspresso,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
        };

        var num = new NumericUpDown
        {
            Location = new Point(x, y + 22),
            Width = 135,
            DecimalPlaces = 1,
            Minimum = 0,
            Maximum = 120,
            Value = defaultVal,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };

        parent.Controls.AddRange(new Control[] { lbl, num });
        return num;
    }

    private void PopulateFields(Customer c)
    {
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

    private async Task SaveCustomerAsync()
    {
        var fName = txtFirstName.Text.Trim();
        var lName = txtLastName.Text.Trim();
        var phone = txtPhone.Text.Trim();

        if (string.IsNullOrWhiteSpace(fName) || string.IsNullOrWhiteSpace(lName))
        {
            MessageBox.Show("First Name and Last Name are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnSave.Enabled = false;

        try
        {
            await using var db = _dbFactory();
            Customer targetCustomer;

            if (!_customerId.HasValue)
            {
                var generator = new CustomerCodeGenerator(_dbFactory);
                string newCode = await generator.GenerateNextCodeAsync(_companyId);

                targetCustomer = new Customer
                {
                    CompanyId = _companyId,
                    CustomerCode = newCode,
                    FirstName = fName,
                    MiddleName = txtMiddleName.Text.Trim(),
                    LastName = lName,
                    ContactNumber = phone,
                    EmailAddress = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    BustSize = numBust.Value,
                    WaistSize = numWaist.Value,
                    HipSize = numHips.Value,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.Customers.Add(targetCustomer);
            }
            else
            {
                var existing = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == _customerId.Value);
                if (existing == null)
                {
                    MessageBox.Show("The customer record could not be found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnSave.Enabled = true;
                    return;
                }

                existing.FirstName = fName;
                existing.MiddleName = txtMiddleName.Text.Trim();
                existing.LastName = lName;
                existing.ContactNumber = phone;
                existing.EmailAddress = txtEmail.Text.Trim();
                existing.Address = txtAddress.Text.Trim();
                existing.BustSize = numBust.Value;
                existing.WaistSize = numWaist.Value;
                existing.HipSize = numHips.Value;

                targetCustomer = existing;
            }

            await db.SaveChangesAsync();
            CreatedCustomerId = targetCustomer.CustomerId;

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