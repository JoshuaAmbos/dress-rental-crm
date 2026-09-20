using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Controls;
using CRM.winforms.Forms;
using CRM.winforms.models;
using CRM.winforms.Models;
using CRM.winforms.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM.winforms.Views;

public partial class Step1ClientSelectionView : UserControl, IBookingWizardStep
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly CustomerProfileService _customerService;
    private readonly Func<int> _getCompanyId;

    // Controls instantiated in code (works even if designer canvas is empty)
    private SearchBar searchBarClients = null!;
    private ListBox listBoxCustomers = null!;
    private Button btnQuickAdd = null!;
    private Customer? _selectedCustomer;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSelectedBg = Color.FromArgb(254, 242, 243);
    private static readonly Color ColorAvatarBg = Color.FromArgb(240, 233, 234);
    private static readonly Color ColorAvatarText = Color.FromArgb(142, 108, 114);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorDivider = Color.FromArgb(242, 235, 235);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);

    public string StepTitle => "Client Selection";

    public Step1ClientSelectionView(Func<TenantCrmDbContext> contextFactory, Func<int> getCompanyId)
    {
        InitializeComponent();

        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _customerService = new CustomerProfileService(_contextFactory);

        BuildStep1Layout();
    }

    private void BuildStep1Layout()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;
        Padding = new Padding(32, 20, 32, 20);

        // 1. Search Bar Top
        searchBarClients = new SearchBar
        {
            Dock = DockStyle.Top,
            Height = 38
        };
        searchBarClients.SetCueBanner("Search by client name, code, or phone number...");
        searchBarClients.SearchTextChanged += async (s, e) => await LoadClientsAsync(searchBarClients.TextValue);

        var pnlTopSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

        // 2. Quick-Add Footer Button
        btnQuickAdd = new Button
        {
            Text = "+ Quick-Add New Client",
            Dock = DockStyle.Bottom,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(254, 250, 250),
            ForeColor = ColorDustyRose,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnQuickAdd.FlatAppearance.BorderColor = ColorBorder;
        btnQuickAdd.Click += BtnQuickAdd_Click;

        var pnlBottomSpacer = new Panel { Dock = DockStyle.Bottom, Height = 14 };

        // 3. Card Wrapper with Border for ListBox
        var pnlListBorder = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = ColorBorder
        };

        // 4. Owner-Drawn Client List
        listBoxCustomers = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 62,
            IntegralHeight = false
        };
        listBoxCustomers.DrawItem += ListBoxCustomers_DrawItem;
        listBoxCustomers.SelectedIndexChanged += ListBoxCustomers_SelectedIndexChanged;

        pnlListBorder.Controls.Add(listBoxCustomers);

        // Add to view in correct docking order
        Controls.Add(pnlListBorder);
        Controls.Add(pnlBottomSpacer);
        Controls.Add(btnQuickAdd);
        Controls.Add(pnlTopSpacer);
        Controls.Add(searchBarClients);
    }

    public async Task LoadClientsAsync(string search = "")
    {
        if (DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            return;

        try
        {
            var customers = await _customerService.GetCustomersAsync(_getCompanyId(), showArchived: false, search);

            listBoxCustomers.BeginUpdate();
            listBoxCustomers.Items.Clear();

            foreach (var c in customers)
            {
                listBoxCustomers.Items.Add(c);
            }

            if (listBoxCustomers.Items.Count > 0)
            {
                listBoxCustomers.SelectedIndex = 0;
            }

            listBoxCustomers.EndUpdate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load clients: {ex.GetBaseException().Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ListBoxCustomers_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= listBoxCustomers.Items.Count || e.Graphics == null)
            return;

        if (listBoxCustomers.Items[e.Index] is not CustomerRowViewModel item)
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bounds = e.Bounds;

        // Background
        using (var bgBrush = new SolidBrush(isSelected ? ColorSelectedBg : Color.White))
        {
            g.FillRectangle(bgBrush, bounds);
        }

        // Left Pink Accent Bar (Selected Only)
        if (isSelected)
        {
            using var barBrush = new SolidBrush(ColorDustyRose);
            g.FillRectangle(barBrush, new Rectangle(bounds.Left, bounds.Top, 4, bounds.Height));
        }

        // Row Divider
        using (var dividerPen = new Pen(ColorDivider, 1f))
        {
            g.DrawLine(dividerPen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        }

        // Avatar Circle with Initials
        int avatarDiameter = 36;
        int avatarX = bounds.Left + 16;
        int avatarY = bounds.Top + (bounds.Height - avatarDiameter) / 2;
        var avatarRect = new Rectangle(avatarX, avatarY, avatarDiameter, avatarDiameter);

        using (var avatarBrush = new SolidBrush(ColorAvatarBg))
        {
            g.FillEllipse(avatarBrush, avatarRect);
        }

        string initials = GetInitials(item.Name);
        using (var avatarFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold))
        {
            TextRenderer.DrawText(g, initials, avatarFont, avatarRect, ColorAvatarText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // Name & Subtitle
        int textX = avatarX + avatarDiameter + 14;
        int textY = bounds.Top + 12;

        using (var nameFont = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold))
        using (var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
        {
            TextRenderer.DrawText(g, item.Name, nameFont, new Point(textX, textY), ColorEspresso);
            string subtitle = $"{item.Code} · {(string.IsNullOrWhiteSpace(item.Phone) ? "—" : item.Phone)}";
            TextRenderer.DrawText(g, subtitle, subFont, new Point(textX, textY + 18), ColorSubtext);
        }

        // Measurements & Checkmark (Right Aligned)
        string sizeText = FormatMeasurements(item);
        using (var sizeFont = new Font("Segoe UI", 9f, FontStyle.Regular))
        using (var checkFont = new Font("Segoe UI Semibold", 10f, FontStyle.Bold))
        {
            int rightOffset = bounds.Right - 16;

            if (isSelected)
            {
                var checkSize = TextRenderer.MeasureText(g, "✓", checkFont);
                rightOffset -= checkSize.Width;
                TextRenderer.DrawText(g, "✓", checkFont, new Point(rightOffset, bounds.Top + (bounds.Height - checkSize.Height) / 2), ColorDustyRose);
                rightOffset -= 10;
            }

            var sizeMetrics = TextRenderer.MeasureText(g, sizeText, sizeFont);
            rightOffset -= sizeMetrics.Width;
            TextRenderer.DrawText(g, sizeText, sizeFont, new Point(rightOffset, bounds.Top + (bounds.Height - sizeMetrics.Height) / 2), ColorSubtext);
        }
    }

    private void ListBoxCustomers_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (listBoxCustomers.SelectedItem is CustomerRowViewModel selected)
        {
            _selectedCustomer = selected.CustomerEntity;
        }
    }

    private async void BtnQuickAdd_Click(object? sender, EventArgs e)
    {
        using var dialog = new CustomerDialogForm(_contextFactory, _getCompanyId());
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.CreatedCustomerId.HasValue)
        {
            await LoadClientsAsync();
            for (int i = 0; i < listBoxCustomers.Items.Count; i++)
            {
                if (listBoxCustomers.Items[i] is CustomerRowViewModel row && row.CustomerId == dialog.CreatedCustomerId.Value)
                {
                    listBoxCustomers.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private static string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "--";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string FormatMeasurements(CustomerRowViewModel item)
    {
        string b = item.BustSize.Replace(" in", "").Trim();
        string w = item.WaistSize.Replace(" in", "").Trim();
        string h = item.HipSize.Replace(" in", "").Trim();

        if (b == "—" && w == "—" && h == "—") return "—";
        return $"{b} – {w} – {h} in";
    }

    // --- IBookingWizardStep Members ---

    public void OnStepEnter(BookingDraftModel draft)
    {
        _selectedCustomer = draft.SelectedCustomer;
        _ = LoadClientsAsync();
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        draft.SelectedCustomer = _selectedCustomer;
    }

    public bool ValidateStep(out string errorMessage)
    {
        if (_selectedCustomer == null)
        {
            errorMessage = "Please select a client profile to proceed.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private void Step1ClientSelectionView_Load(object sender, EventArgs e)
    {

    }

    private void searchBar1_Load(object sender, EventArgs e)
    {

    }
}