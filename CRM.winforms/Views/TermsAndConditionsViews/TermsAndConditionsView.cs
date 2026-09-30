using CRM.domain.Constants;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Drawing2D;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class TermsAndConditionsView : UserControl
{
    private const string ConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

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

    // Parameterless constructor for WinForms Designer
    public TermsAndConditionsView() : this(() =>
    {
        var opt = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TenantCrmDbContext(opt);
    }, new[] { "Admin" })
    {
    }

    public TermsAndConditionsView(Func<TenantCrmDbContext> contextFactory, string[] userRoles)
    {
        _controller = new TermsController(contextFactory ?? throw new ArgumentNullException(nameof(contextFactory)));
        _userRoles = userRoles ?? Array.Empty<string>();

        _canEdit = _userRoles.Any(r =>
            r.Equals(AppRoles.Superadmin, StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase));

        InitializeLayout();
        _ = RefreshDataAsync();
    }

    private void InitializeLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 24, 32, 24);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header Section
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.Transparent
        };

        var lblHeading = new Label
        {
            Text = "Rental Terms & Liability Policies",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };

        lblStatusBadge = new Label
        {
            Text = "Loading policy details...",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(0, 34),
            AutoSize = true
        };

        pnlHeader.Controls.AddRange(new Control[] { lblHeading, lblStatusBadge });

        // 2. Main Content Split (Left: History Archive, Right: Editor / Reader)
        var pnlContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 410,
            SplitterWidth = 14,
            BackColor = Color.Transparent
        };

        // --- LEFT PANEL: REVISION ARCHIVE ---
        var pnlLeftCard = CreateCardPanel();
        pnlLeftCard.Dock = DockStyle.Fill;

        var lblHistTitle = new Label
        {
            Text = "POLICY REVISION ARCHIVE",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Top,
            Height = 28
        };

        dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = ColorCardBg,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = ColorDivider,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 42 },
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 38,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        };

        dgvHistory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorMutedLabel,
            SelectionBackColor = ColorCardBg,
            SelectionForeColor = ColorMutedLabel,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0)
        };

        dgvHistory.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorCardBg,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(8, 0, 8, 0)
        };

        dgvHistory.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorRowAlt,
            ForeColor = ColorBrandDark,
            SelectionBackColor = ColorRowSelected,
            SelectionForeColor = ColorRowSelectedText,
            Font = new Font("Segoe UI", 9.25f),
            Padding = new Padding(8, 0, 8, 0)
        };

        ConfigureHistoryGridColumns();
        dgvHistory.SelectionChanged += DgvHistory_SelectionChanged;
        dgvHistory.CellPainting += DgvHistory_CellPainting;

        btnActivate = new Button
        {
            Text = "Set Selected as Active Agreement",
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = ColorActivePill,
            ForeColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnActivate.FlatAppearance.BorderSize = 1;
        btnActivate.FlatAppearance.BorderColor = ColorBorder;
        btnActivate.MouseEnter += (s, e) => btnActivate.BackColor = ColorSelectedBg;
        btnActivate.MouseLeave += (s, e) => btnActivate.BackColor = ColorActivePill;
        btnActivate.Click += async (s, e) => await ActivateSelectedVersionAsync();

        pnlLeftCard.Controls.Add(dgvHistory);
        pnlLeftCard.Controls.Add(btnActivate);
        pnlLeftCard.Controls.Add(lblHistTitle);
        split.Panel1.Controls.Add(pnlLeftCard);

        // --- RIGHT PANEL: AGREEMENT EDITOR & ACTIONS ---
        var pnlRightCard = CreateCardPanel();
        pnlRightCard.Dock = DockStyle.Fill;

        lblEditorMode = new Label
        {
            Text = "MODE: VIEWING AGREEMENT",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(20, 16),
            AutoSize = true
        };

        btnNew = new Button
        {
            Text = "+ New Draft",
            Font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
            ForeColor = ColorAccent,
            BackColor = ColorActivePill,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(110, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 130, 12),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnNew.FlatAppearance.BorderSize = 1;
        btnNew.FlatAppearance.BorderColor = ColorBorder;
        btnNew.MouseEnter += (s, e) => btnNew.BackColor = ColorSelectedBg;
        btnNew.MouseLeave += (s, e) => btnNew.BackColor = ColorActivePill;
        btnNew.Click += (s, e) => PrepareNewDraft();

        var lblTitleTag = new Label
        {
            Text = "POLICY DOCUMENT TITLE",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(20, 48),
            AutoSize = true
        };

        txtTitle = new TextBox
        {
            Location = new Point(20, 68),
            Width = 380,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorBrandDark,
            ReadOnly = !_canEdit
        };

        var lblVerTag = new Label
        {
            Text = "VERSION",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(415, 48),
            AutoSize = true
        };

        txtVersion = new TextBox
        {
            Location = new Point(415, 68),
            Width = 100,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorBrandDark,
            ReadOnly = !_canEdit
        };

        var lblContentTag = new Label
        {
            Text = "AGREEMENT TERMS & CONDITIONS",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(20, 102),
            AutoSize = true
        };

        lblWordCount = new Label
        {
            Text = "0 words",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5f),
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
            ForeColor = ColorBrandDark,
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
            BackColor = ColorCloseBtnBg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 320, pnlRightCard.Height - 48),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnSaveEdit.FlatAppearance.BorderSize = 1;
        btnSaveEdit.FlatAppearance.BorderColor = ColorBorder;
        btnSaveEdit.MouseEnter += (s, e) => btnSaveEdit.BackColor = ColorActivePill;
        btnSaveEdit.MouseLeave += (s, e) => btnSaveEdit.BackColor = ColorCloseBtnBg;
        btnSaveEdit.Click += async (s, e) => await SaveChangesInPlaceAsync();

        // Publish as New Version (Primary Accent Action)
        btnPublish = new Button
        {
            Text = "Publish as New Version",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(170, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(pnlRightCard.Width - 180, pnlRightCard.Height - 48),
            Cursor = Cursors.Hand,
            Visible = _canEdit
        };
        btnPublish.FlatAppearance.BorderSize = 0;
        btnPublish.MouseEnter += (s, e) => btnPublish.BackColor = ColorAccentHover;
        btnPublish.MouseLeave += (s, e) => btnPublish.BackColor = ColorAccent;
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

    private void ConfigureHistoryGridColumns()
    {
        dgvHistory.Columns.Clear();

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RentalTermId",
            DataPropertyName = "RentalTermId",
            Visible = false
        });

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Version",
            DataPropertyName = "Version",
            HeaderText = "VER",
            Width = 55,
            DefaultCellStyle = { Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) }
        });

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Title",
            DataPropertyName = "Title",
            HeaderText = "TITLE",
            FillWeight = 140,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            DataPropertyName = "Status",
            HeaderText = "STATUS",
            Width = 95
        });

        dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Date",
            DataPropertyName = "Date",
            HeaderText = "EFFECTIVE",
            Width = 90
        });

        foreach (DataGridViewColumn col in dgvHistory.Columns)
        {
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    private void DgvHistory_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Graphics == null) return;

        if (dgvHistory.Columns[e.ColumnIndex].Name == "Status" && e.Value is string status)
        {
            e.PaintBackground(e.CellBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isActive = string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
            Color bg = isActive ? ColorBadgeBg : ColorActivePill;
            Color fg = isActive ? ColorSuccess : ColorNavInactiveText;

            var rect = new Rectangle(e.CellBounds.Left + 4, e.CellBounds.Top + 9, 82, 24);
            using (var brush = new SolidBrush(bg))
            using (var path = CreateRoundedPath(rect, 4))
            {
                e.Graphics.FillPath(brush, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                status,
                new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            e.Handled = true;
        }
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

            var active = terms.FirstOrDefault(t => t.IsActive);
            if (active != null)
            {
                lblStatusBadge.Text = $"Active Policy: v{active.VersionNumber} • Enforced since {active.EffectiveDate:MMM dd, yyyy}";
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
        btnSaveEdit.Enabled = false;
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
            BackColor = ColorCardBg,
            Padding = new Padding(18)
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