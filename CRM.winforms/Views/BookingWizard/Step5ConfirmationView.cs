using System.Drawing.Drawing2D;
using CRM.winforms.Models;

namespace CRM.winforms.Views;

public partial class Step5ConfirmationView : UserControl, IBookingWizardStep
{
    // 1. State & Draft
    private BookingDraftModel? _draft;

    // 2. UI Value Labels (Right Column)
    private Label lblBookingIdVal = null!;
    private Label lblClientVal = null!;
    private Label lblGarmentVal = null!;
    private Label lblPeriodVal = null!;
    private Label lblRentalFeeVal = null!;
    private Label lblDepositVal = null!;
    private Label lblTotalVal = null!;
    private Label lblPaymentMethodVal = null!;

    // 3. Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorDivider = Color.FromArgb(242, 235, 235);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    private const string CurrencySymbol = "$";
    private const int RowHeight = 50;
    private const int TotalRows = 8;

    // 4. Wizard Step Title
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

        // 1. Header Title (No Subtitle)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Review & Confirm",
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };

        // 2. Main Confirmation Card
        var pnlCard = BuildReviewCard();

        // Assembly
        Controls.Add(pnlCard);
        Controls.Add(pnlHeaderSpacer);
        Controls.Add(pnlHeader);
    }

    private Panel BuildReviewCard()
    {
        int cardHeight = (RowHeight * TotalRows) + 4;

        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = cardHeight,
            BackColor = ColorCardBg,
            Padding = new Padding(24, 0, 24, 0)
        };

        // Draw card border and horizontal dividers between each row
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer rounded border
            using (var borderPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8))
            using (var borderPen = new Pen(ColorBorder, 1.25f))
            {
                e.Graphics.DrawPath(borderPen, borderPath);
            }

            // Divider lines between rows
            using (var dividerPen = new Pen(ColorDivider, 1f))
            {
                for (int i = 1; i < TotalRows; i++)
                {
                    int y = i * RowHeight;
                    e.Graphics.DrawLine(dividerPen, 24, y, card.Width - 24, y);
                }
            }
        };

        // Build the 8 rows
        AddRow(card, 0, "Booking ID", out lblBookingIdVal);
        AddRow(card, 1, "Client", out lblClientVal);
        AddRow(card, 2, "Garment", out lblGarmentVal);
        AddRow(card, 3, "Rental Period", out lblPeriodVal);
        AddRow(card, 4, "Rental Fee", out lblRentalFeeVal);
        AddRow(card, 5, "Security Deposit", out lblDepositVal);
        AddRow(card, 6, "Total", out lblTotalVal);
        AddRow(card, 7, "Payment Method", out lblPaymentMethodVal);

        return card;
    }

    private void AddRow(Panel container, int rowIndex, string labelText, out Label valLabel)
    {
        int y = rowIndex * RowHeight;

        // Left Label (Subtext color, regular font)
        var lblTag = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 9.75f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Location = new Point(24, y + 14),
            AutoSize = true
        };

        // Right Label (Espresso color, Semibold font)
        valLabel = new Label
        {
            Text = "—",
            Font = new Font("Segoe UI Semibold", 10.25f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(container.Width - 524, y + 14),
            Size = new Size(500, 24),
            TextAlign = ContentAlignment.MiddleRight
        };

        container.Controls.Add(lblTag);
        container.Controls.Add(valLabel);
    }

    // --- IBookingWizardStep Implementation ---

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;

        // 1. Booking ID (Draft preview indicator)
        lblBookingIdVal.Text = "Pending (Auto-generated)";

        // 2. Client Name
        if (_draft.SelectedCustomer is { } c)
        {
            lblClientVal.Text = $"{c.FirstName} {c.LastName}".Trim();
        }
        else
        {
            lblClientVal.Text = "—";
        }

        // 3. Garment(s)
        if (_draft.SelectedGarments.Count == 1)
        {
            lblGarmentVal.Text = _draft.SelectedGarments[0].StyleName;
        }
        else if (_draft.SelectedGarments.Count > 1)
        {
            lblGarmentVal.Text = string.Join(", ", _draft.SelectedGarments.Select(g => g.StyleName));
        }
        else
        {
            lblGarmentVal.Text = "None Selected";
        }

        // 4. Rental Period
        int days = _draft.RentalDurationDays;
        lblPeriodVal.Text = $"{_draft.RentalStartDate:yyyy-MM-dd} → {_draft.RentalEndDate:yyyy-MM-dd} ({days} day{(days == 1 ? "" : "s")})";

        // 5. Rental Fee
        lblRentalFeeVal.Text = $"{CurrencySymbol}{_draft.TotalRentalFee:N0}";

        // 6. Security Deposit
        lblDepositVal.Text = $"{CurrencySymbol}{_draft.TotalSecurityDeposit:N0}";

        // 7. Total
        lblTotalVal.Text = $"{CurrencySymbol}{_draft.TotalDue:N0}";

        // 8. Payment Method
        lblPaymentMethodVal.Text = string.IsNullOrWhiteSpace(_draft.SelectedPaymentMethod)
            ? "Not specified"
            : _draft.SelectedPaymentMethod;
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        // Data is already finalized in the draft
    }

    public bool ValidateStep(out string errorMessage)
    {
        errorMessage = string.Empty;
        return true;
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