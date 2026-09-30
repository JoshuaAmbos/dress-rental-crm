using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using CRM.infrastructure.data;
using CRM.winforms.Forms;
using CRM.winforms.Models;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class Step5ConfirmationView : UserControl, IBookingWizardStep
{
    private const string DefaultConnectionString =
        "Server=10.0.2.2,1433;Database=DB_TenantCRM;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;";

    private readonly Func<TenantCrmDbContext> _contextFactory;
    private BookingDraftModel? _draft;

    private Label lblBookingIdVal = null!;
    private Label lblClientVal = null!;
    private Label lblGarmentVal = null!;
    private Label lblPeriodVal = null!;
    private Label lblRentalFeeVal = null!;
    private Label lblLoyaltyDiscountVal = null!;
    private Label lblDepositVal = null!;
    private Label lblTotalVal = null!;
    private Label lblPaymentMethodVal = null!;

    private CheckBox chkAgreeTerms = null!;

    private const string CurrencySymbol = "₱";
    private const int RowHeight = 44;
    private const int TotalRows = 9;

    public string StepTitle => "Review & Confirm";

    public Step5ConfirmationView() : this(() =>
    {
        var options = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(DefaultConnectionString)
            .Options;
        return new TenantCrmDbContext(options);
    })
    {
    }

    public Step5ConfirmationView(Func<TenantCrmDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        BuildLayout();
    }

    private void BuildLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header (Title only - no subtitle, matching Steps 1–4)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Review & Confirm",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };

        // 2. Main Summary Card (Calculated height for 9 rows)
        var pnlCard = BuildReviewCard();

        var pnlTermsSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };

        // 3. Terms & Conditions Agreement Checkbox
        var pnlTerms = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 0)
        };

        chkAgreeTerms = new CheckBox
        {
            Text = "Client agrees to rental terms, return deadlines, and security deposit policies. (Click to review)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        chkAgreeTerms.Click += ChkAgreeTerms_Click;

        pnlTerms.Controls.Add(chkAgreeTerms);

        // Assembly (Dock Stacking Order)
        Controls.Add(pnlTerms);
        Controls.Add(pnlTermsSpacer);
        Controls.Add(pnlCard);
        Controls.Add(pnlHeaderSpacer);
        Controls.Add(pnlHeader);
    }

    private void ChkAgreeTerms_Click(object? sender, EventArgs e)
    {
        if (chkAgreeTerms.Checked)
        {
            // Revert state until explicitly accepted in the active terms modal[cite: 2]
            chkAgreeTerms.Checked = false;

            using var termsDialog = new ActiveTermsDialogForm(_contextFactory);
            var result = termsDialog.ShowDialog(this.FindForm() ?? (IWin32Window)this);

            if (result == DialogResult.OK)
            {
                chkAgreeTerms.Checked = true;
                if (_draft != null)
                {
                    _draft.AgreedToTerms = true;
                }
            }
            else
            {
                chkAgreeTerms.Checked = false;
                if (_draft != null)
                {
                    _draft.AgreedToTerms = false;
                }
            }
        }
        else
        {
            if (_draft != null)
            {
                _draft.AgreedToTerms = false;
            }
        }
    }

    private Panel BuildReviewCard()
    {
        int cardHeight = (RowHeight * TotalRows) + 2;

        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = cardHeight,
            BackColor = ColorCardBg,
            Padding = new Padding(24, 0, 24, 0)
        };

        // Reposition labels on resize so they remain visible regardless of form size
        card.Resize += (s, e) =>
        {
            foreach (Control ctrl in card.Controls)
            {
                if (ctrl is Label lbl && lbl.TextAlign == ContentAlignment.MiddleRight)
                {
                    lbl.Left = Math.Max(220, card.ClientSize.Width - lbl.Width - 24);
                }
            }
            card.Invalidate();
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var borderPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var borderPen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(borderPen, borderPath);

            using var dividerPen = new Pen(ColorDivider, 1f);
            for (int i = 1; i < TotalRows; i++)
            {
                int y = i * RowHeight;
                e.Graphics.DrawLine(dividerPen, 24, y, card.Width - 24, y);
            }
        };

        AddRow(card, 0, "Booking ID", out lblBookingIdVal);
        AddRow(card, 1, "Client", out lblClientVal);
        AddRow(card, 2, "Garment", out lblGarmentVal);
        AddRow(card, 3, "Rental Period", out lblPeriodVal);
        AddRow(card, 4, "Base Rental Fee", out lblRentalFeeVal);
        AddRow(card, 5, "Loyalty Discount", out lblLoyaltyDiscountVal);
        AddRow(card, 6, "Security Deposit", out lblDepositVal);
        AddRow(card, 7, "Total Due", out lblTotalVal);
        AddRow(card, 8, "Payment Method", out lblPaymentMethodVal);

        return card;
    }

    private void AddRow(Panel container, int rowIndex, string labelText, out Label valLabel)
    {
        int y = rowIndex * RowHeight;

        var lblTag = new Label
        {
            Text = labelText,
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Location = new Point(24, y + 11),
            AutoSize = true
        };

        valLabel = new Label
        {
            Text = "—",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Size = new Size(500, 22),
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(Math.Max(220, container.Width - 524), y + 11)
        };

        container.Controls.Add(lblTag);
        container.Controls.Add(valLabel);
    }

    // --- IBookingWizardStep Implementation ---

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;

        lblBookingIdVal.Text = "Pending (Auto-generated)";

        string clientName = _draft.SelectedCustomer is { } c ? $"{c.FirstName} {c.LastName}".Trim() : "";
        lblClientVal.Text = string.IsNullOrWhiteSpace(clientName) ? "—" : clientName;

        lblGarmentVal.Text = _draft.SelectedGarments.Count switch
        {
            1 => _draft.SelectedGarments[0].StyleName,
            > 1 => string.Join(", ", _draft.SelectedGarments.Select(g => g.StyleName)),
            _ => "None Selected"
        };

        int days = _draft.RentalDurationDays;
        lblPeriodVal.Text = $"{_draft.RentalStartDate:yyyy-MM-dd} → {_draft.RentalEndDate:yyyy-MM-dd} ({days} day{(days == 1 ? "" : "s")})";
        lblRentalFeeVal.Text = $"{CurrencySymbol}{_draft.SubtotalRentalFee:N2}";

        if (_draft.LoyaltyDiscountAmount > 0)
        {
            lblLoyaltyDiscountVal.ForeColor = ColorSuccess;
            lblLoyaltyDiscountVal.Text = $"-{CurrencySymbol}{_draft.LoyaltyDiscountAmount:N2} ({_draft.LoyaltyTierName} {_draft.LoyaltyDiscountPercentage:0.#}%)";
        }
        else
        {
            lblLoyaltyDiscountVal.ForeColor = ColorSubtext;
            lblLoyaltyDiscountVal.Text = "₱0.00 (Standard Tier)";
        }

        lblDepositVal.Text = $"{CurrencySymbol}{_draft.TotalSecurityDeposit:N2}";

        lblTotalVal.Text = $"{CurrencySymbol}{_draft.TotalDue:N2}";
        lblTotalVal.ForeColor = ColorAccent;
        lblTotalVal.Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);

        lblPaymentMethodVal.Text = string.IsNullOrWhiteSpace(_draft.SelectedPaymentMethod) ? "Not specified" : _draft.SelectedPaymentMethod;

        chkAgreeTerms.Checked = _draft.AgreedToTerms;
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        draft.AgreedToTerms = chkAgreeTerms.Checked;
    }

    public bool ValidateStep(out string errorMessage)
    {
        if (!chkAgreeTerms.Checked)
        {
            errorMessage = "The client must review and agree to the active rental terms and conditions before confirming the booking.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}