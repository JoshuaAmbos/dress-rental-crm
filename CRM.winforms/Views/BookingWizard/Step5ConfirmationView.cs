using CRM.winforms.Models;
using System.Drawing.Drawing2D;

namespace CRM.winforms.Views;

public partial class Step5ConfirmationView : UserControl, IBookingWizardStep
{
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

    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorDivider = Color.FromArgb(242, 235, 235);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorSuccess = Color.FromArgb(5, 150, 105);

    private const string CurrencySymbol = "₱";
    private const int RowHeight = 44;
    private const int TotalRows = 9;

    public string StepTitle => "Review & Confirm";

    public Step5ConfirmationView()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.Transparent };
        var lblTitle = new Label
        {
            Text = "Review & Confirm",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Color.Transparent };

        var pnlTerms = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 0)
        };

        chkAgreeTerms = new CheckBox
        {
            Text = "Client agrees to rental terms, return deadlines, and security deposit policies.",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand
        };
        pnlTerms.Controls.Add(chkAgreeTerms);

        var pnlTermsSpacer = new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Color.Transparent };
        var pnlCard = BuildReviewCard();

        Controls.Add(pnlTermsSpacer);
        Controls.Add(pnlTerms);
        Controls.Add(pnlCard);
        Controls.Add(pnlHeaderSpacer);
        Controls.Add(pnlHeader);
    }

    private Panel BuildReviewCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ColorCardBg,
            Padding = new Padding(24, 0, 24, 0)
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
            ForeColor = ColorEspresso,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(container.Width - 524, y + 11),
            Size = new Size(500, 22),
            TextAlign = ContentAlignment.MiddleRight
        };

        container.Controls.Add(lblTag);
        container.Controls.Add(valLabel);
    }

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;

        lblBookingIdVal.Text = "Pending (Auto-generated)";
        lblClientVal.Text = _draft.SelectedCustomer is { } c ? $"{c.FirstName} {c.LastName}".Trim() : "—";
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

        lblDepositVal.Text = $"{CurrencySymbol}{_draft.TotalSecurityDeposit:N2} ({_draft.DepositPercentage:0.#}%)";
        lblTotalVal.Text = $"{CurrencySymbol}{_draft.TotalDue:N2}";
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
            errorMessage = "The client must agree to the terms and conditions before confirming the booking.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Right - d, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}