using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.domain.entities;
using CRM.infrastructure.data;
using CRM.winforms.Forms;

namespace CRM.winforms.Forms;

public partial class RentalBookingDialogForm : Form
{
    private readonly Func<TenantCrmDbContext> _contextFactory;
    private readonly int _companyId;

    // Atelier Color Palette
    private static readonly Color ColorPrimary = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorAccent = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorBgSoft = Color.FromArgb(253, 250, 249);

    // UI Controls
    private ComboBox cmbCustomer = null!;
    private Button btnAddNewCustomer = null!;
    private CheckedListBox clbGarments = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private TextBox txtAlterationNotes = null!;
    private CheckBox chkAgreedToTerms = null!;
    private Label lblTotalRental = null!;
    private Label lblTotalDeposit = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    // Cached data
    private List<Customer> _customers = new();
    private List<Garment> _availableGarments = new();

    public int? CreatedBookingId { get; private set; }

    public RentalBookingDialogForm(Func<TenantCrmDbContext> contextFactory, int companyId)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _companyId = companyId;

        InitializeCustomComponents();
        this.Load += RentalBookingDialogForm_Load;
    }

    private void InitializeCustomComponents()
    {
        this.Text = "Create New Rental Booking";
        this.Size = new Size(680, 720);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header Panel
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = ColorBgSoft,
            Padding = new Padding(24, 16, 24, 16)
        };
        var lblTitle = new Label
        {
            Text = "New Wardrobe Booking",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            AutoSize = true,
            Location = new Point(24, 14)
        };
        var lblSubtitle = new Label
        {
            Text = "Select an existing client profile and allocate available garments.",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(25, 42)
        };
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

        // Body Content Panel
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 16, 28, 16),
            AutoScroll = true
        };

        int top = 12;

        // 1. Customer Selection (Required)
        var lblCust = new Label { Text = "Client Profile *", Location = new Point(28, top), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
        top += 24;

        cmbCustomer = new ComboBox
        {
            Location = new Point(28, top),
            Width = 470,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10f)
        };

        btnAddNewCustomer = new Button
        {
            Text = "+ New Client",
            Location = new Point(508, top - 1),
            Width = 120,
            Height = 31,
            BackColor = ColorBgSoft,
            ForeColor = ColorAccent,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAddNewCustomer.FlatAppearance.BorderColor = ColorAccent;
        btnAddNewCustomer.Click += BtnAddNewCustomer_Click;

        top += 44;

        // 2. Garments CheckList (Available garments only)
        var lblGarments = new Label { Text = "Select Garment(s) to Rent *", Location = new Point(28, top), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
        top += 24;

        clbGarments = new CheckedListBox
        {
            Location = new Point(28, top),
            Width = 600,
            Height = 150,
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9.25f),
            BorderStyle = BorderStyle.FixedSingle
        };
        clbGarments.ItemCheck += ClbGarments_ItemCheck;
        top += 160;

        // 3. Dates (Start & Return)
        var lblDates = new Label { Text = "Rental Period *", Location = new Point(28, top), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
        top += 24;

        var lblStart = new Label { Text = "Start Date:", Location = new Point(28, top + 4), AutoSize = true, ForeColor = Color.Gray };
        dtpStartDate = new DateTimePicker
        {
            Location = new Point(105, top),
            Width = 180,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        var lblEnd = new Label { Text = "Return Date:", Location = new Point(320, top + 4), AutoSize = true, ForeColor = Color.Gray };
        dtpEndDate = new DateTimePicker
        {
            Location = new Point(410, top),
            Width = 180,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(3)
        };
        top += 38;

        // 4. Alteration / Fitting Notes
        var lblNotes = new Label { Text = "Alteration & Fitting Notes", Location = new Point(28, top), AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold) };
        top += 24;

        txtAlterationNotes = new TextBox
        {
            Location = new Point(28, top),
            Width = 600,
            Height = 60,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "e.g., Hemming 1 inch, bust tightening, minor shoulder take-in..."
        };
        top += 70;

        // 5. Financial Summary Box
        var pnlSummary = new Panel
        {
            Location = new Point(28, top),
            Width = 600,
            Height = 50,
            BackColor = ColorBgSoft,
            BorderStyle = BorderStyle.FixedSingle
        };
        lblTotalRental = new Label
        {
            Text = "Total Rental Rate: ₱0.00",
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Location = new Point(14, 14),
            AutoSize = true
        };
        lblTotalDeposit = new Label
        {
            Text = "Security Deposit: ₱0.00",
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(320, 14),
            AutoSize = true
        };
        pnlSummary.Controls.AddRange(new Control[] { lblTotalRental, lblTotalDeposit });
        top += 62;

        // 6. Agreement Terms
        chkAgreedToTerms = new CheckBox
        {
            Text = "Client has agreed to Rental Agreement Terms & Return Policies",
            Location = new Point(28, top),
            Width = 600,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ColorPrimary
        };
        top += 36;

        pnlBody.Controls.AddRange(new Control[]
        {
            lblCust, cmbCustomer, btnAddNewCustomer,
            lblGarments, clbGarments,
            lblDates, lblStart, dtpStartDate, lblEnd, dtpEndDate,
            lblNotes, txtAlterationNotes,
            pnlSummary, chkAgreedToTerms
        });

        // Footer Actions Panel
        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = ColorBgSoft,
            Padding = new Padding(24, 12, 28, 12)
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 36),
            Location = new Point(410, 12),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.FlatAppearance.BorderColor = ColorBorder;

        btnSave = new Button
        {
            Text = "Confirm Booking",
            Size = new Size(140, 36),
            Location = new Point(520, 12),
            BackColor = ColorAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnSave });

        this.Controls.Add(pnlBody);
        this.Controls.Add(pnlFooter);
        this.Controls.Add(pnlHeader);
    }

    private async void RentalBookingDialogForm_Load(object? sender, EventArgs e)
    {
        await LoadCustomersAsync();
        await LoadAvailableGarmentsAsync();
    }

    public async Task LoadCustomersAsync(int? selectCustomerId = null)
    {
        await using var db = _contextFactory();

        _customers = await db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyId == _companyId && c.IsActive)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();

        cmbCustomer.Items.Clear();

        if (_customers.Count == 0)
        {
            cmbCustomer.Items.Add("-- No customers found (Click '+ New Client' to create) --");
            cmbCustomer.SelectedIndex = 0;
            return;
        }

        foreach (var c in _customers)
        {
            cmbCustomer.Items.Add($"{c.LastName}, {c.FirstName} ({c.ContactNumber})");
        }

        if (selectCustomerId.HasValue)
        {
            int index = _customers.FindIndex(c => c.CustomerId == selectCustomerId.Value);
            cmbCustomer.SelectedIndex = index >= 0 ? index : 0;
        }
        else
        {
            cmbCustomer.SelectedIndex = 0;
        }
    }

    private async Task LoadAvailableGarmentsAsync()
    {
        await using var db = _contextFactory();

        // Only garments that are active and currently marked "Available"
        _availableGarments = await db.Garments
            .AsNoTracking()
            .Where(g => g.CompanyId == _companyId && g.IsActive && g.Status == "Available")
            .OrderBy(g => g.ItemCode)
            .ToListAsync();

        clbGarments.Items.Clear();
        foreach (var g in _availableGarments)
        {
            clbGarments.Items.Add($"[{g.ItemCode}] {g.StyleName} (Size {g.Size}) - ₱{g.RentalRate:N0} (Dep: ₱{g.SecurityDeposit:N0})");
        }

        RecalculateTotals();
    }

    private void ClbGarments_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // Delay recalculation until the checked state finishes updating
        this.BeginInvoke(new Action(RecalculateTotals));
    }

    private void RecalculateTotals()
    {
        decimal totalRental = 0m;
        decimal totalDeposit = 0m;

        for (int i = 0; i < clbGarments.Items.Count; i++)
        {
            if (clbGarments.GetItemChecked(i) && i < _availableGarments.Count)
            {
                var garment = _availableGarments[i];
                totalRental += garment.RentalRate;
                totalDeposit += garment.SecurityDeposit;
            }
        }

        lblTotalRental.Text = $"Total Rental Rate: ₱{totalRental:N2}";
        lblTotalDeposit.Text = $"Security Deposit: ₱{totalDeposit:N2}";
    }

    private async void BtnAddNewCustomer_Click(object? sender, EventArgs e)
    {
        // Opens your existing Customer Profile modal dialog
        using var custDialog = new CustomerDialogForm(_contextFactory, _companyId);
        if (custDialog.ShowDialog(this) == DialogResult.OK)
        {
            // Reload the dropdown and immediately select the newly added client
            await LoadCustomersAsync(custDialog.CreatedCustomerId);
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        // 1. Validation: Customer must exist
        if (_customers.Count == 0 || cmbCustomer.SelectedIndex < 0)
        {
            MessageBox.Show("A valid client profile is required. Please create or select a customer first.", "Customer Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            cmbCustomer.Focus();
            return;
        }

        var selectedCustomer = _customers[cmbCustomer.SelectedIndex];

        // 2. Validation: At least one garment chosen
        var selectedGarments = new List<Garment>();
        for (int i = 0; i < clbGarments.Items.Count; i++)
        {
            if (clbGarments.GetItemChecked(i) && i < _availableGarments.Count)
            {
                selectedGarments.Add(_availableGarments[i]);
            }
        }

        if (selectedGarments.Count == 0)
        {
            MessageBox.Show("Please select at least one garment for this booking.", "Garment Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            clbGarments.Focus();
            return;
        }

        // 3. Validation: Rental Dates
        if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
        {
            MessageBox.Show("The return date cannot be earlier than the rental start date.", "Invalid Dates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            dtpEndDate.Focus();
            return;
        }

        // 4. Validation: Terms
        if (!chkAgreedToTerms.Checked)
        {
            MessageBox.Show("The client must agree to the rental and return terms before proceeding.", "Terms Unchecked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            chkAgreedToTerms.Focus();
            return;
        }

        // 5. Transactional Save
        btnSave.Enabled = false;
        try
        {
            await using var db = _contextFactory();

            decimal totalRental = selectedGarments.Sum(g => g.RentalRate);
            decimal totalDeposit = selectedGarments.Sum(g => g.SecurityDeposit);

            // Create Booking Header
            var booking = new RentalBooking
            {
                CompanyId = _companyId,
                CustomerId = selectedCustomer.CustomerId,
                RentalStartDate = dtpStartDate.Value.Date,
                RentalEndDate = dtpEndDate.Value.Date,
                RentalFee = totalRental,
                SecurityDeposit = totalDeposit,
                AlterationNotes = string.IsNullOrWhiteSpace(txtAlterationNotes.Text) ? null : txtAlterationNotes.Text.Trim(),
                BookingStage = "Reserved",
                AgreedToTerms = true,
                CreatedAt = DateTime.UtcNow
            };

            // Add Line Items & Update Garment Statuses
            var garmentIds = selectedGarments.Select(g => g.GarmentId).ToList();
            var garmentsToUpdate = await db.Garments.Where(g => garmentIds.Contains(g.GarmentId)).ToListAsync();

            foreach (var g in garmentsToUpdate)
            {
                booking.BookingDetails.Add(new BookingDetail
                {
                    GarmentId = g.GarmentId,
                    UnitPrice = g.RentalRate,
                    AlterationNotes = booking.AlterationNotes
                });

                // Transition garment inventory status
                g.Status = "Rented";
            }

            db.RentalBookings.Add(booking);
            await db.SaveChangesAsync();

            CreatedBookingId = booking.RentalBookingId;

            MessageBox.Show(
                $"Booking #{booking.RentalBookingId} for {selectedCustomer.FirstName} {selectedCustomer.LastName} successfully confirmed!",
                "Booking Confirmed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            string realError = ex.GetBaseException().Message;
            MessageBox.Show($"Failed to save booking: {realError}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }
}