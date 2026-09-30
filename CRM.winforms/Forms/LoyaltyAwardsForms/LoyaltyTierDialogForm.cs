using CRM.domain.entities;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class LoyaltyTierDialogForm : Form
{
    private readonly LoyaltyAward? _existingTier;
    public LoyaltyAward TierModel { get; private set; } = null!;

    private TextBox txtTierName = null!;
    private NumericUpDown numMinSpend = null!;
    private NumericUpDown numMinRentals = null!;
    private NumericUpDown numDiscount = null!;
    private TextBox txtRewardDesc = null!;
    private CheckBox chkIsActive = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    public LoyaltyTierDialogForm(LoyaltyAward? existingTier = null)
    {
        _existingTier = existingTier;

        BuildLayout();
        PopulateFields();
    }

    private void BuildLayout()
    {
        Text = _existingTier == null ? "Add Loyalty Tier" : "Modify Loyalty Tier";
        Size = new Size(500, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(28, 20, 28, 20);

        var lblTitle = new Label
        {
            Text = _existingTier == null ? "New Loyalty Tier" : "Edit Loyalty Tier",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 18),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Configure spend thresholds, leasing milestones, and automated discounts.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(28, 48),
            AutoSize = true
        };
        Controls.AddRange(new Control[] { lblTitle, lblSub });

        int y = 82;

        // 1. Tier Name
        txtTierName = CreateTextField("TIER IDENTIFIER / NAME *", ref y, "e.g., Diamond, Platinum, Elite VIP");

        // 2. Numeric inputs (Spend & Leases side by side)
        var pnlNumbers = new TableLayoutPanel
        {
            Location = new Point(28, y),
            Size = new Size(428, 62),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pnlNumbers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        pnlNumbers.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var pnlSpend = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        var lblSpend = new Label { Text = "MIN SPEND (₱) *", Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = ColorMutedLabel, Dock = DockStyle.Top, Height = 18 };
        numMinSpend = new NumericUpDown
        {
            Dock = DockStyle.Top,
            Height = 32,
            Maximum = 1000000,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Font = new Font("Segoe UI", 10f),
            BackColor = ColorCardBg
        };
        pnlSpend.Controls.AddRange(new Control[] { numMinSpend, lblSpend });

        var pnlRentals = new Panel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        var lblRentals = new Label { Text = "MIN COMPLETED LEASES *", Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = ColorMutedLabel, Dock = DockStyle.Top, Height = 18 };
        numMinRentals = new NumericUpDown
        {
            Dock = DockStyle.Top,
            Height = 32,
            Maximum = 500,
            Font = new Font("Segoe UI", 10f),
            BackColor = ColorCardBg
        };
        pnlRentals.Controls.AddRange(new Control[] { numMinRentals, lblRentals });

        pnlNumbers.Controls.Add(pnlSpend, 0, 0);
        pnlNumbers.Controls.Add(pnlRentals, 1, 0);
        Controls.Add(pnlNumbers);
        y += 66;

        // 3. Discount Percentage
        var lblDisc = new Label { Text = "PERK DISCOUNT PERCENTAGE (%) *", Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = ColorMutedLabel, Location = new Point(28, y), AutoSize = true };
        numDiscount = new NumericUpDown
        {
            Location = new Point(28, y + 20),
            Width = 428,
            Maximum = 100,
            DecimalPlaces = 1,
            Font = new Font("Segoe UI", 10f),
            BackColor = ColorCardBg
        };
        Controls.AddRange(new Control[] { lblDisc, numDiscount });
        y += 60;

        // 4. Description
        var lblDesc = new Label { Text = "REWARD DESCRIPTION & MEMBER PERKS", Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold), ForeColor = ColorMutedLabel, Location = new Point(28, y), AutoSize = true };
        txtRewardDesc = new TextBox
        {
            Location = new Point(28, y + 20),
            Width = 428,
            Height = 54,
            Multiline = true,
            Font = new Font("Segoe UI", 9.5f),
            BackColor = ColorCardBg,
            PlaceholderText = "e.g., Free alterations, 10% lease discount, priority reservations"
        };
        Controls.AddRange(new Control[] { lblDesc, txtRewardDesc });
        y += 84;

        // 5. Active Status Checkbox
        chkIsActive = new CheckBox
        {
            Text = "Tier is active and available for client qualification",
            Checked = true,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, y),
            Size = new Size(428, 26),
            Cursor = Cursors.Hand
        };
        Controls.Add(chkIsActive);

        // Action Toolbar
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };

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
            Text = _existingTier == null ? "Create Tier" : "Save Changes",
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
        btnSave.Click += BtnSave_Click;

        pnlActions.Controls.AddRange(new Control[] { btnCancel, btnSave });
        Controls.Add(pnlActions);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private TextBox CreateTextField(string label, ref int yPos, string placeholder)
    {
        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(28, yPos),
            AutoSize = true
        };

        var pnl = new Panel
        {
            Location = new Point(28, yPos + 20),
            Size = new Size(428, 34),
            BackColor = ColorCardBg,
            Padding = new Padding(8, 6, 8, 4)
        };
        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1.2f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 4);
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

        pnl.Controls.Add(txt);
        Controls.AddRange(new Control[] { lbl, pnl });

        yPos += 60;
        return txt;
    }

    private void PopulateFields()
    {
        if (_existingTier != null)
        {
            txtTierName.Text = _existingTier.TierName;
            numMinSpend.Value = _existingTier.MinLifetimeSpend;
            numMinRentals.Value = _existingTier.MinRentalCount;
            numDiscount.Value = _existingTier.DiscountPercentage;
            txtRewardDesc.Text = _existingTier.RewardDescription;
            chkIsActive.Checked = _existingTier.IsActive;

            if (_existingTier.TierName.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            {
                txtTierName.Enabled = false;
                numMinSpend.Enabled = false;
                numMinRentals.Enabled = false;
                chkIsActive.Enabled = false;
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTierName.Text))
        {
            MessageBox.Show("Please specify a Tier Name (e.g., VIP, Gold, Diamond).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtTierName.Focus();
            return;
        }

        TierModel = _existingTier ?? new LoyaltyAward();
        TierModel.TierName = txtTierName.Text.Trim();
        TierModel.MinLifetimeSpend = numMinSpend.Value;
        TierModel.MinRentalCount = (int)numMinRentals.Value;
        TierModel.DiscountPercentage = numDiscount.Value;
        TierModel.RewardDescription = txtRewardDesc.Text.Trim();
        TierModel.IsActive = chkIsActive.Checked;

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