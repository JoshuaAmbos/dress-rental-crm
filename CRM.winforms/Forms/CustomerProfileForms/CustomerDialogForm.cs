using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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

    // Inputs
    private TextBox txtFirstName = null!;
    private TextBox txtMiddleName = null!;
    private TextBox txtLastName = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtAddress = null!;
    private NumericUpDown numBust = null!;
    private NumericUpDown numWaist = null!;
    private NumericUpDown numHips = null!;

    // Buttons
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Button btnClose = null!;

    private Point _dragStartPoint;

    // Atelier Palette (High-Definition Borders)
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorDustyRoseHover = Color.FromArgb(171, 99, 108);
    private static readonly Color ColorSectionTag = Color.FromArgb(180, 95, 105);
    private static readonly Color ColorSubtext = Color.FromArgb(130, 120, 125);
    private static readonly Color ColorFormBorder = Color.FromArgb(170, 150, 155); // Prominent dialog frame
    private static readonly Color ColorInputBorder = Color.FromArgb(204, 188, 184); // Defined input outline
    private static readonly Color ColorDivider = Color.FromArgb(220, 208, 205);
    private static readonly Color ColorInputBg = Color.White;
    private static readonly Color ColorCloseBtnBg = Color.FromArgb(246, 240, 238);

    public CustomerDialogForm(Func<TenantCrmDbContext> dbFactory, int companyId)
        : this(dbFactory, companyId, null)
    {
    }

    public CustomerDialogForm(Func<TenantCrmDbContext> dbFactory, int companyId, Customer? existingCustomer)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _companyId = companyId;
        _customerId = existingCustomer?.CustomerId;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(580, 650);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        if (existingCustomer != null)
        {
            _existingCustomerCode = existingCustomer.CustomerCode;
        }

        ApplyFormBorderAndRegion();
        BuildModernForm();

        if (existingCustomer != null)
        {
            PopulateFields(existingCustomer);
        }
    }

    private void ApplyFormBorderAndRegion()
    {
        Resize += (s, e) =>
        {
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), 16);
            Region = new Region(path);
            Invalidate();
        };

        Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorFormBorder, 2f);
            using var path = CreateRoundedRectangle(new Rectangle(1, 1, Width - 3, Height - 3), 16);
            e.Graphics.DrawPath(pen, path);
        };
    }

    private void BuildModernForm()
    {
        // 1. Header (Inset 2px so border is never clipped)
        var pnlHeader = new Panel
        {
            Location = new Point(2, 2),
            Size = new Size(Width - 4, 78),
            BackColor = Color.White
        };

        pnlHeader.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) _dragStartPoint = e.Location; };
        pnlHeader.MouseMove += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                Left += e.X - _dragStartPoint.X;
                Top += e.Y - _dragStartPoint.Y;
            }
        };

        var lblTitle = new Label
        {
            Text = _customerId.HasValue ? "Edit Client Profile" : "New Client Intake",
            Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(28, 18),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = _customerId.HasValue && !string.IsNullOrEmpty(_existingCustomerCode)
                ? $"Client Code: {_existingCustomerCode}"
                : "Client measurement & fit preference sheet",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(30, 48),
            AutoSize = true
        };

        btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Size = new Size(32, 32),
            Location = new Point(pnlHeader.Width - 48, 18),
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCloseBtnBg,
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(p, 28, 77, pnlHeader.Width - 28, 77);
        };

        pnlHeader.Controls.AddRange([lblTitle, lblSubtitle, btnClose]);
        Controls.Add(pnlHeader);

        // 2. Form Body
        int y = 96;
        int col1X = 28;
        int col2X = 300;
        int colWidth = 250;

        AddSectionHeader("PERSONAL DETAILS", 28, ref y);
        AddLabeledInput("First Name *", "e.g. Isabella", col1X, y, colWidth, out txtFirstName);
        AddLabeledInput("Last Name *", "e.g. Rosario", col2X, y, colWidth, out txtLastName);
        y += 66;

        AddLabeledInput("Phone Number *", "+1 (212) 555-0000", col1X, y, colWidth, out txtPhone);
        AddLabeledInput("Middle Name (Optional)", "e.g. Marie", col2X, y, colWidth, out txtMiddleName);
        y += 66;

        AddLabeledInput("Email Address", "name@email.com", col1X, y, colWidth, out txtEmail);
        AddLabeledInput("City / Location", "New York, NY", col2X, y, colWidth, out txtAddress);
        y += 76;

        AddSectionHeader("MEASUREMENTS (INCHES)", 28, ref y);
        int measWidth = 164;
        int gap = 16;

        AddLabeledNumericInput("Bust", 34.0m, col1X, y, measWidth, out numBust);
        AddLabeledNumericInput("Waist", 26.0m, col1X + measWidth + gap, y, measWidth, out numWaist);
        AddLabeledNumericInput("Hip", 36.0m, col1X + (measWidth + gap) * 2, y, measWidth, out numHips);
        y += 84;

        // 3. Footer
        var pnlFooter = new Panel
        {
            Location = new Point(2, Height - 74),
            Size = new Size(Width - 4, 72),
            BackColor = Color.White
        };

        pnlFooter.Paint += (s, e) =>
        {
            using var p = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(p, 28, 0, pnlFooter.Width - 28, 0);
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(100, 38),
            Location = new Point(pnlFooter.Width - 250, 16),
            BackColor = Color.White,
            ForeColor = ColorEspresso,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ColorInputBorder;
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        btnSave = new Button
        {
            Text = _customerId.HasValue ? "Update Client" : "Create Client",
            Size = new Size(130, 38),
            Location = new Point(pnlFooter.Width - 140, 16),
            BackColor = ColorDustyRose,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = ColorDustyRoseHover;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = ColorDustyRose;
        btnSave.Click += async (s, e) => await SaveCustomerAsync();

        pnlFooter.Controls.AddRange([btnCancel, btnSave]);
        Controls.Add(pnlFooter);
    }

    private void AddSectionHeader(string text, int x, ref int y)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = ColorSectionTag,
            Location = new Point(x, y),
            AutoSize = true
        };
        Controls.Add(lbl);
        y += 24;
    }

    private void AddLabeledInput(string labelText, string placeholder, int x, int y, int width, out TextBox textBox)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(x, y),
            AutoSize = true
        };

        var container = new Panel
        {
            Location = new Point(x, y + 20),
            Size = new Size(width, 36),
            BackColor = ColorInputBg
        };

        container.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorInputBorder, 1.5f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, container.Width - 1, container.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };

        textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            PlaceholderText = placeholder,
            Location = new Point(10, 8),
            Width = width - 20
        };

        container.Controls.Add(textBox);
        Controls.AddRange([lbl, container]);
    }

    private void AddLabeledNumericInput(string labelText, decimal defaultValue, int x, int y, int width, out NumericUpDown num)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(x, y),
            AutoSize = true
        };

        var container = new Panel
        {
            Location = new Point(x, y + 20),
            Size = new Size(width, 36),
            BackColor = ColorInputBg
        };

        container.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorInputBorder, 1.5f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, container.Width - 1, container.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };

        num = new NumericUpDown
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            DecimalPlaces = 1,
            Minimum = 0,
            Maximum = 120,
            Value = defaultValue,
            TextAlign = HorizontalAlignment.Left,
            Location = new Point(10, 8),
            Width = width - 20
        };

        container.Controls.Add(num);
        Controls.AddRange([lbl, container]);
    }

    private void PopulateFields(Customer c)
    {
        txtFirstName.Text = c.FirstName ?? string.Empty;
        txtMiddleName.Text = c.MiddleName ?? string.Empty;
        txtLastName.Text = c.LastName ?? string.Empty;
        txtPhone.Text = c.ContactNumber ?? string.Empty;
        txtEmail.Text = c.EmailAddress ?? string.Empty;
        txtAddress.Text = c.Address ?? string.Empty;

        numBust.Value = Math.Clamp(c.BustSize, numBust.Minimum, numBust.Maximum);
        numWaist.Value = Math.Clamp(c.WaistSize, numWaist.Minimum, numWaist.Maximum);
        numHips.Value = Math.Clamp(c.HipSize, numHips.Minimum, numHips.Maximum);
    }

    private async Task SaveCustomerAsync()
    {
        var fName = txtFirstName.Text.Trim();
        var lName = txtLastName.Text.Trim();
        var phone = txtPhone.Text.Trim();

        if (string.IsNullOrWhiteSpace(fName) || string.IsNullOrWhiteSpace(lName) || string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("Please fill in all required fields marked with *.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
        if (cleanPhone.Length < 7 || cleanPhone.Length > 15)
        {
            MessageBox.Show("Please enter a valid contact phone number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPhone.Focus();
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

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}