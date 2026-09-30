using CRM.infrastructure.data;
using CRM.winforms.Services;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Forms;

public class SuperadminReportExportDialog : Form
{
    private readonly SuperadminReportPrinter _printer;
    private readonly string _currentUsername;

    private RadioButton radAll = null!;
    private RadioButton radTenants = null!;
    private RadioButton radDatabases = null!;
    private RadioButton radUsers = null!;
    private Button btnPreview = null!;
    private Button btnCancel = null!;

    public SuperadminReportExportDialog(
        Func<MasterCrmDbContext> masterDbFactory,
        Func<TenantCrmDbContext> tenantDbFactory,
        string currentUsername = "superadmin")
    {
        _printer = new SuperadminReportPrinter(masterDbFactory, tenantDbFactory);
        _currentUsername = currentUsername;

        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "System Audit Reports";
        Size = new Size(520, 430);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(28, 20, 28, 20);

        var lblTitle = new Label
        {
            Text = "Audit & Compliance Reports",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(28, 16),
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Preview and print system reports or export them directly to PDF.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorSubtext,
            Location = new Point(28, 44),
            AutoSize = true
        };
        Controls.AddRange(new Control[] { lblTitle, lblSub });

        // Category Selection Card
        var pnlCard = new Panel
        {
            Location = new Point(28, 76),
            Size = new Size(448, 230),
            BackColor = ColorCardBg,
            Padding = new Padding(16, 12, 16, 12)
        };
        pnlCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

        var lblCardTitle = new Label
        {
            Text = "SELECT AUDIT CATEGORY",
            Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(14, 10),
            AutoSize = true
        };
        pnlCard.Controls.Add(lblCardTitle);

        radAll = CreateCategoryRadio("Consolidated Master Audit (All)", "Complete overview across tenants, databases, and users.", 34, true);
        radTenants = CreateCategoryRadio("Boutique Tenants & Licensing Ledger", "Tenant list, monthly fees, and subscription packages.", 80);
        radDatabases = CreateCategoryRadio("Database Infrastructure & Routing Map", "Server connections, tenant database routing, and online states.", 126);
        radUsers = CreateCategoryRadio("Global User Accounts & Security Directory", "Cross-tenant identity roster, assigned roles, and showrooms.", 172);

        pnlCard.Controls.AddRange(new Control[] { radAll, radTenants, radDatabases, radUsers });
        Controls.Add(pnlCard);

        // Action Toolbar
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(95, 36),
            Location = new Point(pnlActions.Width - 250, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorCardBg,
            ForeColor = ColorNavInactiveText,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.BorderColor = ColorBorder;

        btnPreview = new Button
        {
            Text = "Print / Export PDF",
            Size = new Size(145, 36),
            Location = new Point(pnlActions.Width - 145, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = ColorAccent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnPreview.FlatAppearance.BorderSize = 0;
        btnPreview.Click += async (s, e) => await ExecutePreviewAsync();

        pnlActions.Controls.AddRange(new Control[] { btnCancel, btnPreview });
        Controls.Add(pnlActions);

        AcceptButton = btnPreview;
        CancelButton = btnCancel;
    }

    private RadioButton CreateCategoryRadio(string title, string subtitle, int top, bool isChecked = false)
    {
        return new RadioButton
        {
            Text = $"{title}\n   {subtitle}",
            Checked = isChecked,
            Location = new Point(14, top),
            Size = new Size(420, 40),
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Cursor = Cursors.Hand
        };
    }

    private async Task ExecutePreviewAsync()
    {
        string categoryTitle = "Master Audit Dossier";

        if (radTenants.Checked)
        {
            categoryTitle = "Boutique Tenants";
        }
        else if (radDatabases.Checked)
        {
            categoryTitle = "Database Routing";
        }
        else if (radUsers.Checked)
        {
            categoryTitle = "User Accounts";
        }

        try
        {
            btnPreview.Enabled = false;
            btnPreview.Text = "Loading...";

            this.Hide();
            await _printer.OpenReportPreviewAsync(categoryTitle, _currentUsername, this.Owner ?? this);

            DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            this.Show();
            MessageBox.Show($"Failed to generate preview: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnPreview.Enabled = true;
            btnPreview.Text = "Print / Export PDF";
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