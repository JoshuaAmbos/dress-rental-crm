using CRM.domain.entities;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public partial class BranchDialogForm : Form
{
    private readonly Branch? _existingBranch;
    private readonly int _companyId;

    public Branch BranchModel { get; private set; } = null!;

    private TextBox txtBranchCode = null!;
    private TextBox txtBranchName = null!;
    private TextBox txtCity = null!;
    private TextBox txtAddress = null!;
    private TextBox txtContactPhone = null!;
    private CheckBox chkIsActive = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public BranchDialogForm(int companyId, Branch? existingBranch = null)
    {
        _companyId = companyId;
        _existingBranch = existingBranch;

        BuildLayout();
        PopulateData();
    }

    private void BuildLayout()
    {
        Text = _existingBranch == null ? "Register Showroom Location" : "Edit Showroom Details";
        Size = new Size(540, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(32, 24, 32, 24);

        // Header
        var lblTitle = new Label
        {
            Text = _existingBranch == null ? "New Showroom Location" : "Update Showroom Details",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(32, 24),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Configure showroom code, regional location, and contact parameters.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(32, 54),
            AutoSize = true
        };

        Controls.AddRange(new Control[] { lblTitle, lblSub });

        int startY = 88;
        txtBranchCode = CreateField("SHOWROOM CODE *", ref startY, "e.g., BR-HQ, BR-MKT-01");
        txtBranchName = CreateField("SHOWROOM / ATELIER NAME *", ref startY, "e.g., Flagship Atelier, Galleria Studio");
        txtCity = CreateField("CITY / MUNICIPALITY *", ref startY, "e.g., Makati City, Cebu City, Davao");
        txtAddress = CreateField("STREET ADDRESS", ref startY, "e.g., Suite 402, Fashion Row, Central District");
        txtContactPhone = CreateField("CONTACT PHONE", ref startY, "e.g., +63 2 8123 4567 / +63 917 000 0000");

        chkIsActive = new CheckBox
        {
            Text = "Showroom is active and available for customer bookings",
            Checked = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(32, startY + 6),
            Size = new Size(460, 26),
            Cursor = Cursors.Hand
        };
        Controls.Add(chkIsActive);

        // Bottom Action Toolbar
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Color.Transparent
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 36),
            Location = new Point(pnlActions.Width - 230, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = ColorBorder;

        btnSave = new Button
        {
            Text = _existingBranch == null ? "Save Showroom" : "Save Changes",
            Size = new Size(120, 36),
            Location = new Point(pnlActions.Width - 120, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.MouseEnter += (s, e) => btnSave.BackColor = ColorAccentHover;
        btnSave.MouseLeave += (s, e) => btnSave.BackColor = ColorAccent;
        btnSave.Click += BtnSave_Click;

        pnlActions.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(pnlActions);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private TextBox CreateField(string labelText, ref int yPos, string placeholder)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(32, yPos),
            AutoSize = true
        };

        var pnlBorder = new Panel
        {
            Location = new Point(32, yPos + 18),
            Size = new Size(460, 32),
            BackColor = ColorCardBg,
            Padding = new Padding(8, 6, 8, 4)
        };
        pnlBorder.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.2f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlBorder.Width - 1, pnlBorder.Height - 1), 4);
            e.Graphics.DrawPath(pen, path);
        };

        var txt = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            BackColor = ColorCardBg,
            PlaceholderText = placeholder
        };

        pnlBorder.Controls.Add(txt);
        Controls.AddRange(new Control[] { lbl, pnlBorder });

        yPos += 56;
        return txt;
    }

    private void PopulateData()
    {
        if (_existingBranch != null)
        {
            txtBranchCode.Text = _existingBranch.BranchCode;
            txtBranchName.Text = _existingBranch.BranchName;
            txtCity.Text = _existingBranch.City;
            txtAddress.Text = _existingBranch.Address;
            txtContactPhone.Text = _existingBranch.ContactPhone;
            chkIsActive.Checked = _existingBranch.IsActive;
        }
        else
        {
            txtBranchCode.Text = $"BR-{DateTime.Now:yyMM}-{Random.Shared.Next(100, 999)}";
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtBranchCode.Text))
        {
            MessageBox.Show("Please enter a unique Showroom Code.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBranchCode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtBranchName.Text))
        {
            MessageBox.Show("Please specify a Showroom / Atelier Name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBranchName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtCity.Text))
        {
            MessageBox.Show("Please specify the City / Municipality.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtCity.Focus();
            return;
        }

        BranchModel = _existingBranch ?? new Branch { CompanyId = _companyId };
        BranchModel.BranchCode = txtBranchCode.Text.Trim().ToUpperInvariant();
        BranchModel.BranchName = txtBranchName.Text.Trim();
        BranchModel.City = txtCity.Text.Trim();
        BranchModel.Address = txtAddress.Text.Trim();
        BranchModel.ContactPhone = txtContactPhone.Text.Trim();
        BranchModel.IsActive = chkIsActive.Checked;

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