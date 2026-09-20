using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;

namespace CRM.winforms.Forms;

public partial class InquiryDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _companyId;

    public Inquiry InquiryModel { get; private set; }

    // Client Selector & Inputs
    private ComboBox cmbExistingCustomer = null!;
    private Button btnAddNewCustomer = null!;
    private TextBox txtName = null!;
    private TextBox txtPhone = null!;
    private TextBox txtEmail = null!;
    private TextBox txtBudget = null!;
    private TextBox txtEvent = null!;
    private DateTimePicker dtpEvent = null!;
    private TextBox txtGarment = null!;
    private ComboBox cmbPriority = null!;
    private ComboBox cmbStatus = null!;

    // Action Buttons
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Button btnClose = null!;

    private Point _dragStartPoint;
    private List<Customer> _customers = new();

    // Atelier Palette (High-Definition Borders)
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorDustyRoseHover = Color.FromArgb(171, 99, 108);
    private static readonly Color ColorSectionTag = Color.FromArgb(180, 95, 105);
    private static readonly Color ColorSubtext = Color.FromArgb(130, 120, 125);
    private static readonly Color ColorFormBorder = Color.FromArgb(170, 150, 155);
    private static readonly Color ColorInputBorder = Color.FromArgb(204, 188, 184);
    private static readonly Color ColorDivider = Color.FromArgb(220, 208, 205);
    private static readonly Color ColorInputBg = Color.White;
    private static readonly Color ColorDisabledInputBg = Color.FromArgb(249, 246, 246);
    private static readonly Color ColorCloseBtnBg = Color.FromArgb(246, 240, 238);

    public InquiryDialogForm(Func<TenantCrmDbContext> contextFactory, int companyId, Inquiry? existing = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _companyId = companyId;
        InquiryModel = existing ?? new Inquiry { CompanyId = companyId };

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(580, 750);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        ApplyFormBorderAndRegion();
        BuildModernForm();
        BindData();

        Load += async (s, e) => await LoadCustomersAsync();
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
        // 1. Header (Inset 2px)
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
            Text = string.IsNullOrEmpty(InquiryModel.InquiryCode) ? "New Inquiry Intake" : $"Edit Inquiry • {InquiryModel.InquiryCode}",
            Font = new Font("Segoe UI", 15.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(28, 18),
            AutoSize = true
        };

        var lblSubtitle = new Label
        {
            Text = "Track lead details, event requirements, and wardrobe preferences",
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

        // 2. Scrollable Body
        var pnlBody = new Panel
        {
            Location = new Point(2, 80),
            Size = new Size(Width - 4, Height - 154),
            AutoScroll = true,
            BackColor = Color.White
        };

        int y = 14;
        int col1X = 28;
        int col2X = 295;
        int colWidth = 245;

        // SECTION: CLIENT SELECTION & PROFILE
        AddSectionHeader("CLIENT INFORMATION", col1X, ref y, pnlBody);

        var lblExisting = new Label
        {
            Text = "Select Registered Client (Optional)",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(col1X, y),
            AutoSize = true
        };
        pnlBody.Controls.Add(lblExisting);
        y += 20;

        var pnlCustBox = new Panel { Location = new Point(col1X, y), Size = new Size(385, 36), BackColor = Color.White };
        ApplyPillBorder(pnlCustBox);

        cmbExistingCustomer = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            Location = new Point(8, 6),
            Width = 369
        };
        cmbExistingCustomer.SelectedIndexChanged += CmbExistingCustomer_SelectedIndexChanged;
        pnlCustBox.Controls.Add(cmbExistingCustomer);

        btnAddNewCustomer = new Button
        {
            Text = "+ New Client",
            Location = new Point(col1X + 395, y),
            Size = new Size(117, 36),
            BackColor = Color.White,
            ForeColor = ColorDustyRose,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnAddNewCustomer.FlatAppearance.BorderColor = ColorDustyRose;
        btnAddNewCustomer.FlatAppearance.BorderSize = 1;
        btnAddNewCustomer.Click += BtnAddNewCustomer_Click;

        pnlBody.Controls.AddRange([pnlCustBox, btnAddNewCustomer]);
        y += 50;

        AddLabeledInput("Full Name *", "e.g. Isabella Rosario", col1X, y, colWidth, out txtName, pnlBody);
        AddLabeledInput("Phone Number", "+1 (212) 555-0000", col2X, y, colWidth, out txtPhone, pnlBody);
        y += 66;

        AddLabeledInput("Email Address", "name@email.com", col1X, y, colWidth, out txtEmail, pnlBody);
        AddLabeledInput("Budget Range", "e.g. $400–$600", col2X, y, colWidth, out txtBudget, pnlBody);
        y += 76;

        // SECTION: EVENT & GARMENT REQUEST
        AddSectionHeader("EVENT & GARMENT REQUEST", col1X, ref y, pnlBody);
        AddLabeledInput("Event Type", "e.g. Gala, Wedding, Black-Tie", col1X, y, colWidth, out txtEvent, pnlBody);
        AddLabeledDatePicker("Event Date", col2X, y, colWidth, out dtpEvent, pnlBody);
        y += 66;

        AddLabeledInput("Garment Request / Style Notes", "Floor-length gown, navy or silk velvet...", col1X, y, Width - 58, out txtGarment, pnlBody);
        y += 76;

        // SECTION: PIPELINE STATUS
        AddSectionHeader("PIPELINE & PRIORITY", col1X, ref y, pnlBody);
        AddLabeledComboBox("Priority", ["Low", "Medium", "High"], col1X, y, colWidth, out cmbPriority, pnlBody);
        AddLabeledComboBox("Pipeline Stage", ["New", "In Review", "Quoted", "Converted", "Closed"], col2X, y, colWidth, out cmbStatus, pnlBody);

        Controls.Add(pnlBody);

        // 3. Footer Action Bar
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
            Text = string.IsNullOrEmpty(InquiryModel.InquiryCode) ? "Create Inquiry" : "Save Changes",
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
        btnSave.Click += BtnSave_Click;

        pnlFooter.Controls.AddRange([btnCancel, btnSave]);
        Controls.Add(pnlFooter);
    }

    private void AddSectionHeader(string text, int x, ref int y, Panel parent)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = ColorSectionTag,
            Location = new Point(x, y),
            AutoSize = true
        };
        parent.Controls.Add(lbl);
        y += 24;
    }

    private void AddLabeledInput(string labelText, string placeholder, int x, int y, int width, out TextBox textBox, Panel parent)
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
        ApplyPillBorder(container);

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
        parent.Controls.AddRange([lbl, container]);
    }

    private void AddLabeledDatePicker(string labelText, int x, int y, int width, out DateTimePicker dtp, Panel parent)
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
        ApplyPillBorder(container);

        dtp = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            CalendarForeColor = ColorEspresso,
            Location = new Point(8, 6),
            Width = width - 16
        };

        container.Controls.Add(dtp);
        parent.Controls.AddRange([lbl, container]);
    }

    private void AddLabeledComboBox(string labelText, string[] items, int x, int y, int width, out ComboBox cmb, Panel parent)
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
        ApplyPillBorder(container);

        cmb = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorEspresso,
            Location = new Point(8, 6),
            Width = width - 16
        };
        cmb.Items.AddRange(items);

        container.Controls.Add(cmb);
        parent.Controls.AddRange([lbl, container]);
    }

    private static void ApplyPillBorder(Panel panel)
    {
        panel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorInputBorder, 1.5f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };
    }

    private async Task LoadCustomersAsync(int? selectedCustomerId = null)
    {
        try
        {
            await using var db = _contextFactory();
            _customers = await db.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == _companyId && c.IsActive)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            cmbExistingCustomer.Items.Clear();
            cmbExistingCustomer.Items.Add("-- New / Unregistered Prospect --");

            int selectIndex = 0;
            for (int i = 0; i < _customers.Count; i++)
            {
                var c = _customers[i];
                cmbExistingCustomer.Items.Add($"{c.LastName}, {c.FirstName} ({c.ContactNumber})");

                if (selectedCustomerId.HasValue && c.CustomerId == selectedCustomerId.Value)
                {
                    selectIndex = i + 1;
                }
                else if (!selectedCustomerId.HasValue && !string.IsNullOrEmpty(InquiryModel.ClientName) &&
                         $"{c.FirstName} {c.LastName}".Equals(InquiryModel.ClientName, StringComparison.OrdinalIgnoreCase))
                {
                    selectIndex = i + 1;
                }
            }

            cmbExistingCustomer.SelectedIndex = selectIndex;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load customers: {ex.GetBaseException().Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void CmbExistingCustomer_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbExistingCustomer.SelectedIndex > 0 && cmbExistingCustomer.SelectedIndex - 1 < _customers.Count)
        {
            var customer = _customers[cmbExistingCustomer.SelectedIndex - 1];
            txtName.Text = $"{customer.FirstName} {customer.LastName}".Trim();
            txtPhone.Text = customer.ContactNumber ?? string.Empty;
            txtEmail.Text = customer.EmailAddress ?? string.Empty;

            // Lock fields when prefilled from customer database
            txtName.ReadOnly = true;
            txtName.Parent!.BackColor = ColorDisabledInputBg;
            txtPhone.ReadOnly = true;
            txtPhone.Parent!.BackColor = ColorDisabledInputBg;
            txtEmail.ReadOnly = true;
            txtEmail.Parent!.BackColor = ColorDisabledInputBg;
        }
        else
        {
            // Unlock fields for manual prospect entry
            txtName.ReadOnly = false;
            txtName.Parent!.BackColor = ColorInputBg;
            txtPhone.ReadOnly = false;
            txtPhone.Parent!.BackColor = ColorInputBg;
            txtEmail.ReadOnly = false;
            txtEmail.Parent!.BackColor = ColorInputBg;
        }
    }

    private async void BtnAddNewCustomer_Click(object? sender, EventArgs e)
    {
        using var custDlg = new CustomerDialogForm(_contextFactory, _companyId);
        if (custDlg.ShowDialog(this) == DialogResult.OK)
        {
            await LoadCustomersAsync(custDlg.CreatedCustomerId);
        }
    }

    private void BindData()
    {
        txtName.Text = InquiryModel.ClientName ?? string.Empty;
        txtPhone.Text = InquiryModel.ClientPhone ?? string.Empty;
        txtEmail.Text = InquiryModel.ClientEmail ?? string.Empty;
        txtBudget.Text = InquiryModel.BudgetRange ?? string.Empty;
        txtEvent.Text = InquiryModel.EventType ?? string.Empty;
        if (InquiryModel.EventDate.HasValue) dtpEvent.Value = InquiryModel.EventDate.Value;
        txtGarment.Text = InquiryModel.GarmentRequest ?? string.Empty;

        cmbPriority.SelectedItem = string.IsNullOrEmpty(InquiryModel.Priority) ? "Medium" : InquiryModel.Priority;
        cmbStatus.SelectedItem = string.IsNullOrEmpty(InquiryModel.Status) ? "New" : InquiryModel.Status;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Client Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtName.Focus();
            return;
        }

        InquiryModel.ClientName = txtName.Text.Trim();
        InquiryModel.ClientPhone = txtPhone.Text.Trim();
        InquiryModel.ClientEmail = txtEmail.Text.Trim();
        InquiryModel.BudgetRange = txtBudget.Text.Trim();
        InquiryModel.EventType = txtEvent.Text.Trim();
        InquiryModel.EventDate = dtpEvent.Value.Date;
        InquiryModel.GarmentRequest = txtGarment.Text.Trim();
        InquiryModel.Priority = cmbPriority.SelectedItem?.ToString() ?? "Medium";
        InquiryModel.Status = cmbStatus.SelectedItem?.ToString() ?? "New";

        DialogResult = DialogResult.OK;
        Close();
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