using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public partial class InquiryDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _companyId;
    public Inquiry InquiryModel { get; }

    private TextBox txtClientName = null!;
    private TextBox txtClientPhone = null!;
    private TextBox txtClientEmail = null!;
    private ComboBox cmbBranch = null!;
    private ComboBox cmbEventType = null!;
    private DateTimePicker dtpEventDate = null!;
    private TextBox txtGarmentRequest = null!;
    private ComboBox cmbBudgetRange = null!;
    private ComboBox cmbPriority = null!;
    private ComboBox cmbStatus = null!;
    private TextBox txtNotes = null!;

    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Button btnClose = null!;
    private Point _dragStartPoint;

    public InquiryDialogForm(Func<TenantCrmDbContext> contextFactory, int companyId, Inquiry? existing = null, int? defaultBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _companyId = companyId;
        InquiryModel = existing ?? new Inquiry
        {
            CompanyId = companyId,
            BranchId = defaultBranchId,
            Status = "New",
            Priority = "Medium",
            EventType = "Wedding",
            EventDate = DateTime.Today.AddDays(14)
        };

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(620, 680);
        BackColor = Color.White;
        DoubleBuffered = true;

        BuildFormLayout();
        _ = LoadBranchesAsync(InquiryModel.BranchId ?? defaultBranchId);
        PopulateExisting();
    }

    private void BuildFormLayout()
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
            using var pen = new Pen(Color.FromArgb(170, 150, 155), 2f);
            using var path = CreateRoundedRectangle(new Rectangle(1, 1, Width - 3, Height - 3), 16);
            e.Graphics.DrawPath(pen, path);
        };

        // Header
        var pnlHeader = new Panel { Location = new Point(2, 2), Size = new Size(Width - 4, 76), BackColor = Color.White };
        pnlHeader.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) _dragStartPoint = e.Location; };
        pnlHeader.MouseMove += (s, e) =>
        {
            if (e.Button == MouseButtons.Left) { Left += e.X - _dragStartPoint.X; Top += e.Y - _dragStartPoint.Y; }
        };

        var lblTitle = new Label
        {
            Text = InquiryModel.InquiryId > 0 ? $"Edit Inquiry ({InquiryModel.InquiryCode})" : "Log Inbound Inquiry",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Capture client event preferences, budget guidelines, and garment requests",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorSubtext,
            Location = new Point(30, 46),
            AutoSize = true
        };

        btnClose = new Button
        {
            Text = "✕",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Size = new Size(32, 32),
            Location = new Point(pnlHeader.Width - 46, 16),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(246, 240, 238),
            Cursor = Cursors.Hand
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlHeader.Paint += (s, e) =>
        {
            using var p = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(p, 28, 75, pnlHeader.Width - 28, 75);
        };

        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub, btnClose });
        Controls.Add(pnlHeader);

        // Body Inputs
        int y = 92;
        int col1X = 28;
        int col2X = 320;
        int inputWidth = 270;

        AddLabeledInput("Client Full Name *", col1X, y, inputWidth, out txtClientName);
        AddLabeledInput("Contact Phone Number *", col2X, y, inputWidth, out txtClientPhone);
        y += 62;

        AddLabeledInput("Email Address", col1X, y, inputWidth, out txtClientEmail);
        AddLabeledComboBox("Target Showroom Branch", col2X, y, inputWidth, out cmbBranch);
        y += 62;

        AddLabeledComboBox("Event Type *", col1X, y, inputWidth, out cmbEventType);
        cmbEventType.Items.AddRange(new object[] { "Wedding", "Gala", "Prom / Debut", "Black Tie Awards", "Cocktail Party", "Fashion Editorial", "Anniversary" });
        cmbEventType.SelectedIndex = 0;

        AddLabeledDatePicker("Estimated Event Date", col2X, y, inputWidth, out dtpEventDate);
        y += 62;

        AddLabeledInput("Garment Style Request *", col1X, y, inputWidth, out txtGarmentRequest);

        AddLabeledComboBox("Budget Range", col2X, y, inputWidth, out cmbBudgetRange);
        cmbBudgetRange.Items.AddRange(new object[] { "₱3,000–₱5,000", "₱5,000–₱8,000", "₱8,000–₱12,000", "₱12,000+" });
        cmbBudgetRange.SelectedIndex = 0;
        y += 62;

        AddLabeledComboBox("Priority Level", col1X, y, inputWidth, out cmbPriority);
        cmbPriority.Items.AddRange(new object[] { "Low", "Medium", "High" });
        cmbPriority.SelectedIndex = 1;

        AddLabeledComboBox("Status", col2X, y, inputWidth, out cmbStatus);
        cmbStatus.Items.AddRange(new object[] { "New", "In Review", "Quoted", "Converted", "Closed" });
        cmbStatus.SelectedIndex = 0;
        y += 62;

        AddLabeledMultiline("Consultation & Preference Notes", col1X, y, Width - 56, 75, out txtNotes);

        // Footer
        var pnlFooter = new Panel { Location = new Point(2, Height - 68), Size = new Size(Width - 4, 66), BackColor = Color.White };
        pnlFooter.Paint += (s, e) =>
        {
            using var p = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(p, 28, 0, pnlFooter.Width - 28, 0);
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(95, 36),
            Location = new Point(pnlFooter.Width - 245, 14),
            BackColor = Color.White,
            ForeColor = ColorPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ColorBorder;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        btnSave = new Button
        {
            Text = InquiryModel.InquiryId > 0 ? "Update Inquiry" : "Save Inquiry",
            Size = new Size(135, 36),
            Location = new Point(pnlFooter.Width - 140, 14),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += SaveData;

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(pnlFooter);
    }

    private void AddLabeledInput(string label, int x, int y, int width, out TextBox tb)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        var pnl = new Panel { Location = new Point(x, y + 18), Size = new Size(width, 34), BackColor = Color.White };
        pnl.Paint += (s, e) => { using var p = new Pen(ColorBorder, 1.25f); e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1); };

        tb = new TextBox { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9.5f), Location = new Point(8, 7), Width = width - 16, ForeColor = ColorPrimary };
        pnl.Controls.Add(tb);
        Controls.AddRange(new Control[] { lbl, pnl });
    }

    private void AddLabeledComboBox(string label, int x, int y, int width, out ComboBox cb)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f), Location = new Point(x, y + 18), Width = width, ForeColor = ColorPrimary };
        Controls.AddRange(new Control[] { lbl, cb });
    }

    private void AddLabeledDatePicker(string label, int x, int y, int width, out DateTimePicker dtp)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        dtp = new DateTimePicker { Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9.5f), Location = new Point(x, y + 18), Width = width, CalendarForeColor = ColorPrimary };
        Controls.AddRange(new Control[] { lbl, dtp });
    }

    private void AddLabeledMultiline(string label, int x, int y, int width, int height, out TextBox tb)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        var pnl = new Panel { Location = new Point(x, y + 18), Size = new Size(width, height), BackColor = Color.White };
        pnl.Paint += (s, e) => { using var p = new Pen(ColorBorder, 1.25f); e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1); };

        tb = new TextBox { BorderStyle = BorderStyle.None, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9f), Location = new Point(8, 6), Size = new Size(width - 16, height - 12), ForeColor = ColorPrimary };
        pnl.Controls.Add(tb);
        Controls.AddRange(new Control[] { lbl, pnl });
    }

    private async Task LoadBranchesAsync(int? selectedBranchId)
    {
        try
        {
            await using var db = _contextFactory();
            var branches = await db.Branches.AsNoTracking().Where(b => b.CompanyId == _companyId && b.IsActive).OrderBy(b => b.BranchName).ToListAsync();
            cmbBranch.Items.Clear();
            cmbBranch.Items.Add(new BranchComboItem(null, "🌐 All Showrooms (Central)"));

            int idx = 0;
            for (int i = 0; i < branches.Count; i++)
            {
                cmbBranch.Items.Add(new BranchComboItem(branches[i].BranchId, $"🏢 {branches[i].BranchName}"));
                if (selectedBranchId.HasValue && branches[i].BranchId == selectedBranchId.Value) idx = i + 1;
            }
            if (cmbBranch.Items.Count > 0) cmbBranch.SelectedIndex = idx;
        }
        catch { }
    }

    private void PopulateExisting()
    {
        txtClientName.Text = InquiryModel.ClientName;
        txtClientPhone.Text = InquiryModel.ClientPhone;
        txtClientEmail.Text = InquiryModel.ClientEmail;
        txtGarmentRequest.Text = InquiryModel.GarmentRequest;
        txtNotes.Text = InquiryModel.Notes ?? string.Empty;

        if (InquiryModel.EventDate.HasValue) dtpEventDate.Value = InquiryModel.EventDate.Value;
        if (!string.IsNullOrEmpty(InquiryModel.EventType)) cmbEventType.SelectedItem = InquiryModel.EventType;
        if (!string.IsNullOrEmpty(InquiryModel.BudgetRange)) cmbBudgetRange.SelectedItem = InquiryModel.BudgetRange;
        if (!string.IsNullOrEmpty(InquiryModel.Priority)) cmbPriority.SelectedItem = InquiryModel.Priority;
        if (!string.IsNullOrEmpty(InquiryModel.Status)) cmbStatus.SelectedItem = InquiryModel.Status;
    }

    private void SaveData(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtClientName.Text) || string.IsNullOrWhiteSpace(txtGarmentRequest.Text))
        {
            MessageBox.Show("Please enter the client's name and garment style request.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        InquiryModel.ClientName = txtClientName.Text.Trim();
        InquiryModel.ClientPhone = txtClientPhone.Text.Trim();
        InquiryModel.ClientEmail = txtClientEmail.Text.Trim();
        InquiryModel.BranchId = (cmbBranch.SelectedItem is BranchComboItem item) ? item.BranchId : null;
        InquiryModel.EventType = cmbEventType.SelectedItem?.ToString() ?? "Wedding";
        InquiryModel.EventDate = dtpEventDate.Value.Date;
        InquiryModel.GarmentRequest = txtGarmentRequest.Text.Trim();
        InquiryModel.BudgetRange = cmbBudgetRange.SelectedItem?.ToString() ?? "₱3,000–₱5,000";
        InquiryModel.Priority = cmbPriority.SelectedItem?.ToString() ?? "Medium";
        InquiryModel.Status = cmbStatus.SelectedItem?.ToString() ?? "New";
        InquiryModel.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();

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

    private record BranchComboItem(int? BranchId, string Name) { public override string ToString() => Name; }
}