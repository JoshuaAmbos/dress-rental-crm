using CRM.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Forms;

public partial class ComplaintDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _companyId;
    public Complaint ComplaintModel { get; }

    private TextBox txtClientName = null!;
    private TextBox txtClientPhone = null!;
    private TextBox txtClientEmail = null!;
    private ComboBox cmbBranch = null!;
    private ComboBox cmbCategory = null!;
    private ComboBox cmbSeverity = null!;
    private ComboBox cmbStatus = null!;
    private TextBox txtDescription = null!;
    private TextBox txtResolution = null!;
    private NumericUpDown numCompensation = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Button btnClose = null!;
    private Point _dragStartPoint;

    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(130, 120, 125);
    private static readonly Color ColorBorder = Color.FromArgb(204, 188, 184);
    private static readonly Color ColorDivider = Color.FromArgb(220, 208, 205);

    public ComplaintDialogForm(Func<TenantCrmDbContext> contextFactory, int companyId, Complaint? existing = null, int? defaultBranchId = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _companyId = companyId;
        ComplaintModel = existing ?? new Complaint
        {
            CompanyId = companyId,
            BranchId = defaultBranchId,
            Status = "New",
            Severity = "Medium",
            Category = "Garment Condition"
        };

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(620, 710);
        BackColor = Color.White;
        DoubleBuffered = true;

        BuildFormLayout();
        _ = LoadBranchesAsync(ComplaintModel.BranchId ?? defaultBranchId);
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
            Text = ComplaintModel.ComplaintId > 0 ? $"Edit Complaint ({ComplaintModel.ComplaintCode})" : "Log Client Complaint",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(28, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Incident assessment, customer dispute handling, and resolution logging",
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
        AddLabeledComboBox("Showroom Branch", col2X, y, inputWidth, out cmbBranch);
        y += 62;

        AddLabeledComboBox("Complaint Category *", col1X, y, inputWidth, out cmbCategory);
        cmbCategory.Items.AddRange(new object[] { "Garment Condition", "Fit & Alterations", "Late Delivery", "Customer Service", "Billing Dispute", "Deposit Deduction Issue", "Other" });
        cmbCategory.SelectedIndex = 0;

        AddLabeledComboBox("Severity Level *", col2X, y, inputWidth, out cmbSeverity);
        cmbSeverity.Items.AddRange(new object[] { "Low", "Medium", "High", "Critical" });
        cmbSeverity.SelectedIndex = 1;
        y += 62;

        AddLabeledComboBox("Incident Status *", col1X, y, inputWidth, out cmbStatus);
        cmbStatus.Items.AddRange(new object[] { "New", "Under Investigation", "In Progress", "Resolved", "Escalated" });
        cmbStatus.SelectedIndex = 0;

        AddLabeledNumericInput("Compensation / Refund (₱)", col2X, y, inputWidth, out numCompensation);
        y += 62;

        AddLabeledMultiline("Incident Description / Issue *", col1X, y, Width - 56, 75, out txtDescription);
        y += 105;

        AddLabeledMultiline("Resolution Notes / Corrective Action", col1X, y, Width - 56, 75, out txtResolution);

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
            ForeColor = ColorEspresso,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = ColorBorder;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        btnSave = new Button
        {
            Text = ComplaintModel.ComplaintId > 0 ? "Update Complaint" : "Log Complaint",
            Size = new Size(135, 36),
            Location = new Point(pnlFooter.Width - 140, 14),
            BackColor = ColorDustyRose,
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

        tb = new TextBox { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 9.5f), Location = new Point(8, 7), Width = width - 16, ForeColor = ColorEspresso };
        pnl.Controls.Add(tb);
        Controls.AddRange(new Control[] { lbl, pnl });
    }

    private void AddLabeledComboBox(string label, int x, int y, int width, out ComboBox cb)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f), Location = new Point(x, y + 18), Width = width, ForeColor = ColorEspresso };
        Controls.AddRange(new Control[] { lbl, cb });
    }

    private void AddLabeledNumericInput(string label, int x, int y, int width, out NumericUpDown num)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        num = new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000, Font = new Font("Segoe UI", 9.5f), Location = new Point(x, y + 18), Width = width, ForeColor = ColorEspresso, TextAlign = HorizontalAlignment.Right };
        Controls.AddRange(new Control[] { lbl, num });
    }

    private void AddLabeledMultiline(string label, int x, int y, int width, int height, out TextBox tb)
    {
        var lbl = new Label { Text = label, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = ColorSubtext, Location = new Point(x, y), AutoSize = true };
        var pnl = new Panel { Location = new Point(x, y + 18), Size = new Size(width, height), BackColor = Color.White };
        pnl.Paint += (s, e) => { using var p = new Pen(ColorBorder, 1.25f); e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1); };

        tb = new TextBox { BorderStyle = BorderStyle.None, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9f), Location = new Point(8, 6), Size = new Size(width - 16, height - 12), ForeColor = ColorEspresso };
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
        txtClientName.Text = ComplaintModel.ClientName;
        txtClientPhone.Text = ComplaintModel.ClientPhone ?? string.Empty;
        txtClientEmail.Text = ComplaintModel.ClientEmail ?? string.Empty;
        txtDescription.Text = ComplaintModel.Description;
        txtResolution.Text = ComplaintModel.ResolutionNotes ?? string.Empty;
        numCompensation.Value = Math.Clamp(ComplaintModel.CompensationAmount, 0, 1000000);

        if (!string.IsNullOrEmpty(ComplaintModel.Category)) cmbCategory.SelectedItem = ComplaintModel.Category;
        if (!string.IsNullOrEmpty(ComplaintModel.Severity)) cmbSeverity.SelectedItem = ComplaintModel.Severity;
        if (!string.IsNullOrEmpty(ComplaintModel.Status)) cmbStatus.SelectedItem = ComplaintModel.Status;
    }

    private void SaveData(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtClientName.Text) || string.IsNullOrWhiteSpace(txtDescription.Text))
        {
            MessageBox.Show("Please enter the client's name and incident description.", "Validation Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ComplaintModel.ClientName = txtClientName.Text.Trim();
        ComplaintModel.ClientPhone = txtClientPhone.Text.Trim();
        ComplaintModel.ClientEmail = txtClientEmail.Text.Trim();
        ComplaintModel.BranchId = (cmbBranch.SelectedItem is BranchComboItem item) ? item.BranchId : null;
        ComplaintModel.Category = cmbCategory.SelectedItem?.ToString() ?? "General";
        ComplaintModel.Severity = cmbSeverity.SelectedItem?.ToString() ?? "Medium";
        ComplaintModel.Status = cmbStatus.SelectedItem?.ToString() ?? "New";
        ComplaintModel.Description = txtDescription.Text.Trim();
        ComplaintModel.ResolutionNotes = string.IsNullOrWhiteSpace(txtResolution.Text) ? null : txtResolution.Text.Trim();
        ComplaintModel.CompensationAmount = numCompensation.Value;

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