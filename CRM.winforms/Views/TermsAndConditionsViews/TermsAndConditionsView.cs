using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class TermsAndConditionsView : UserControl
{
    private readonly TermsController _controller;
    private readonly string[] _userRoles;

    private DataGridView dgvHistory = null!;
    private TextBox txtTitle = null!;
    private TextBox txtVersion = null!;
    private TextBox txtContent = null!;
    private Button btnNew = null!;
    private Button btnSaveEdit = null!;
    private Button btnPublish = null!;
    private Button btnActivate = null!;
    private Label lblStatusBadge = null!;
    private Label lblWordCount = null!;
    private Label lblEditorMode = null!;

    private RentalTerm? _selectedTerm;
    private readonly bool _canEdit;

    public TermsAndConditionsView(Func<TenantCrmDbContext> contextFactory, string[] userRoles)
    {
        _controller = new TermsController(contextFactory);
        _userRoles = userRoles ?? Array.Empty<string>();

        _canEdit = _userRoles.Any(r =>
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

        BuildUI();
        _ = RefreshDataAsync();
    }

    private void BuildUI()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(28, 20, 28, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.Transparent
        };

        var lblHeading = new Label
        {
            Text = "Rental Terms & Liability Policies",
            Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblStatusBadge = new Label
        {
            Text = "Loading policy...",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorSubtext,
            Location = new Point(2, 24),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblHeading, lblStatusBadge });

        var pnlContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 380,
            SplitterWidth = 12,
            BackColor = Color.Transparent
        };

        // --- Left: Policy History & Action Column ---
        var pnlLeftCard = CreateCardPanel();
        pnlLeftCard.Dock = DockStyle.Fill;

        var lblHistTitle = new Label
        {
            Text = "POLICY REVISION ARCHIVE",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Top,
            Height = 26
        };

        dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(242, 235, 235),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 36 }
        };
        dgvHistory.EnableHeadersVisualStyles = false;
        dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(253, 248, 248);
        dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = ColorPrimary;
        dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
        dgvHistory.SelectionChanged += DgvHistory_SelectionChanged;

        btnActivate = new Button
        {
            Text = "Set Selected as Active",
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = ColorActivePill,
            ForeColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnActivate.FlatAppearance.BorderSize = 0;
        btnActivate.Click += async (s, e) => await ActivateSelectedVersionAsync();

        pnlLeftCard.Controls.Add(dgvHistory);
        pnlLeftCard.Controls.Add(btnActivate);
        pnlLeftCard.Controls.Add(lblHistTitle);
        split.Panel1.Controls.Add(pnlLeftCard);

        // --- Right: Agreement Editor & Actions ---
        var pnlRightCard = CreateCardPanel();
        pnlRightCard.Dock = DockStyle.Fill;

        lblEditorMode = new Label
        {
            Text = "MODE: VIEWING AGREEMENT",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(20, 16),
            AutoSize = true
        };

        btnNew = new Button
        {
            Text = "+ New Draft",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            BackColor = Color.FromArgb(245, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 130, 12),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnNew.FlatAppearance.BorderSize = 0;
        btnNew.Click += (s, e) => PrepareNewDraft();

        var lblTitleTag = new Label
        {
            Text = "POLICY TITLE",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(20, 48),
            AutoSize = true
        };

        txtTitle = new TextBox
        {
            Location = new Point(20, 68),
            Width = 380,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            ReadOnly = !_canEdit
        };

        var lblVerTag = new Label
        {
            Text = "VERSION",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(415, 48),
            AutoSize = true
        };

        txtVersion = new TextBox
        {
            Location = new Point(415, 68),
            Width = 90,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorPrimary,
            ReadOnly = !_canEdit
        };

        var lblContentTag = new Label
        {
            Text = "AGREEMENT TERMS & CONDITIONS",
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(20, 102),
            AutoSize = true
        };

        lblWordCount = new Label
        {
            Text = "0 words",
            Font = new Font("Segoe UI", 8f),
            ForeColor = ColorSubtext,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 120, 102),
            AutoSize = true
        };

        txtContent = new TextBox
        {
            Location = new Point(20, 124),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorPrimary,
            Height = pnlRightCard.Height - 190,
            Width = pnlRightCard.Width - 40,
            ReadOnly = !_canEdit
        };
        txtContent.TextChanged += (s, e) => UpdateWordCount();

        // Save Changes (In-place edit)
        btnSaveEdit = new Button
        {
            Text = "Save Changes",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            BackColor = Color.FromArgb(244, 238, 238),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 310, pnlRightCard.Height - 48),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnSaveEdit.FlatAppearance.BorderSize = 0;
        btnSaveEdit.Click += async (s, e) => await SaveChangesInPlaceAsync();

        // Publish as New Version
        btnPublish = new Button
        {
            Text = "Publish as New Version",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(160, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 170, pnlRightCard.Height - 48),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnPublish.FlatAppearance.BorderSize = 0;
        btnPublish.Click += async (s, e) => await PublishRevisionAsync();

        pnlRightCard.Controls.AddRange(new Control[]
        {
            lblEditorMode, btnNew,
            lblTitleTag, txtTitle,
            lblVerTag, txtVersion,
            lblContentTag, lblWordCount,
            txtContent,
            btnSaveEdit, btnPublish
        });
        split.Panel2.Controls.Add(pnlRightCard);

        pnlContainer.Controls.Add(split);
        Controls.Add(pnlContainer);
        Controls.Add(pnlHeader);
    }

    public async Task RefreshDataAsync()
    {
        try
        {
            var terms = await _controller.GetAllVersionsAsync();

            dgvHistory.DataSource = terms.Select(t => new
            {
                t.RentalTermId,
                Version = $"v{t.VersionNumber}",
                Title = t.PolicyTitle,
                Status = t.IsActive ? "ACTIVE" : "Superseded",
                Date = t.EffectiveDate.ToString("MMM dd, yyyy")
            }).ToList();

            if (dgvHistory.Columns.Contains("RentalTermId"))
                dgvHistory.Columns["RentalTermId"]!.Visible = false;

            var active = terms.FirstOrDefault(t => t.IsActive);
            if (active != null)
            {
                lblStatusBadge.Text = $"Active Version: v{active.VersionNumber} • Enforced since {active.EffectiveDate:MMM dd, yyyy}";
                DisplayTerm(active);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load terms: {ex.GetBaseException().Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvHistory_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvHistory.SelectedRows.Count == 0) return;

        var termIdVal = dgvHistory.SelectedRows[0].Cells["RentalTermId"].Value;
        if (termIdVal is int termId)
        {
            _ = LoadTermDetailsAsync(termId);
        }
    }

    private async Task LoadTermDetailsAsync(int termId)
    {
        var terms = await _controller.GetAllVersionsAsync();
        var selected = terms.FirstOrDefault(t => t.RentalTermId == termId);
        if (selected != null)
        {
            DisplayTerm(selected);
        }
    }

    private void DisplayTerm(RentalTerm term)
    {
        _selectedTerm = term;
        lblEditorMode.Text = $"MODE: EDITING v{term.VersionNumber} (ID: {term.RentalTermId})";
        txtTitle.Text = term.PolicyTitle;
        txtVersion.Text = term.VersionNumber;
        txtContent.Text = term.PolicyContent;

        btnActivate.Enabled = !term.IsActive && _canEdit;
        btnSaveEdit.Enabled = _canEdit;
        UpdateWordCount();
    }

    private void PrepareNewDraft()
    {
        _selectedTerm = null;
        dgvHistory.ClearSelection();

        lblEditorMode.Text = "MODE: CREATING NEW DRAFT";
        txtTitle.Text = "New Wardrobe Rental Agreement";
        txtVersion.Text = "1.0";
        txtContent.Clear();

        btnActivate.Enabled = false;
        btnSaveEdit.Enabled = false; // Only Publish is valid for a brand new draft
        txtTitle.Focus();
    }

    private async Task SaveChangesInPlaceAsync()
    {
        if (_selectedTerm == null)
        {
            MessageBox.Show("Please select an existing agreement to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtContent.Text))
        {
            MessageBox.Show("Title and content cannot be blank.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnSaveEdit.Enabled = false;
            await _controller.UpdateExistingPolicyAsync(_selectedTerm.RentalTermId, txtTitle.Text, txtVersion.Text, txtContent.Text);
            MessageBox.Show("Changes saved to the current agreement.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSaveEdit.Enabled = true;
        }
    }

    private async Task PublishRevisionAsync()
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtContent.Text))
        {
            MessageBox.Show("Please provide a Policy Title and Contractual Clauses.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnPublish.Enabled = false;
            await _controller.PublishNewVersionAsync(txtTitle.Text, txtVersion.Text, txtContent.Text);
            MessageBox.Show("New policy revision published and set as active.", "Published", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Publish failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnPublish.Enabled = true;
        }
    }

    private async Task ActivateSelectedVersionAsync()
    {
        if (_selectedTerm == null) return;

        var confirm = MessageBox.Show(
            $"Activate Version {_selectedTerm.VersionNumber} as the active boutique agreement?",
            "Confirm Activation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes)
        {
            await _controller.SetActiveVersionAsync(_selectedTerm.RentalTermId);
            await RefreshDataAsync();
        }
    }

    private void UpdateWordCount()
    {
        var words = txtContent.Text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        lblWordCount.Text = $"{words.Length} words";
    }

    private static Panel CreateCardPanel()
    {
        var pnl = new Panel
        {
            BackColor = Color.White,
            Padding = new Padding(16)
        };
        pnl.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(ColorBorder, 1f);
            using var path = CreateRoundedPath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };
        return pnl;
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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