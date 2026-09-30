using CRM.winforms.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using static CRM.winforms.Assets.Themes.ColorThemes;

namespace CRM.winforms.Views;

public partial class Step4PaymentDepositView : UserControl, IBookingWizardStep
{
    private BookingDraftModel? _draft;
    private string _selectedPaymentMethod = "Gcash";

    private readonly List<string> _paymentMethods = new()
    {
        "Gcash",
        "Credit Card – Visa",
        "Credit Card – Mastercard",
        "Credit Card – Amex",
        "Bank Transfer / Instapay",
        "Cash on Pickup"
    };

    private Label lblRentalFeeTitle = null!;
    private Label lblRentalFeeAmount = null!;
    private Label lblDiscountTitle = null!;
    private Label lblDiscountAmount = null!;
    private Label lblDepositTitle = null!;
    private Label lblDepositAmount = null!;
    private Label lblTotalDueTitle = null!;
    private Label lblTotalDueAmount = null!;
    private Label lblDepositNote = null!;
    private PaymentCardListBox listBoxPaymentMethods = null!;

    private const string CurrencySymbol = "₱";

    public string StepTitle => "Payment & Deposit";

    public Step4PaymentDepositView()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Padding = new Padding(32, 20, 32, 20);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Header (Title only - no subtitle, matching Steps 1, 2, & 3)
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Payment & Deposit",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };

        // 2. Fee Breakdown Card
        var pnlFeeCard = BuildFeeBreakdownCard();
        var pnlCardSpacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };

        // 3. Payment Method Section Tag
        var lblMethodHeader = new Label
        {
            Text = "SELECT PAYMENT METHOD",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.BottomLeft
        };

        var pnlMethodSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

        // 4. Owner-Drawn Payment Option Cards
        listBoxPaymentMethods = new PaymentCardListBox
        {
            Dock = DockStyle.Fill,
            BackColor = ColorViewBg,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 48,
            IntegralHeight = false
        };

        foreach (var method in _paymentMethods)
        {
            listBoxPaymentMethods.Items.Add(method);
        }

        listBoxPaymentMethods.SelectedIndex = 0;
        listBoxPaymentMethods.DrawItem += ListBoxPaymentMethods_DrawItem;
        listBoxPaymentMethods.SelectedIndexChanged += (s, e) =>
        {
            if (listBoxPaymentMethods.SelectedItem is string m)
            {
                _selectedPaymentMethod = m;
            }
        };

        // Assembly (Dock Stacking Order)
        Controls.Add(listBoxPaymentMethods);
        Controls.Add(pnlMethodSpacer);
        Controls.Add(lblMethodHeader);
        Controls.Add(pnlCardSpacer);
        Controls.Add(pnlFeeCard);
        Controls.Add(pnlHeaderSpacer);
        Controls.Add(pnlHeader);
    }

    private Panel BuildFeeBreakdownCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            Height = 196,
            BackColor = ColorCardBg,
            Padding = new Padding(24, 16, 24, 16)
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var borderPen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(borderPen, borderPath);

            // Divider line directly above "Total Due"
            using var dividerPen = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(dividerPen, 24, 126, card.Width - 24, 126);
        };

        var lblFeeTag = new Label
        {
            Text = "FEE BREAKDOWN",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorMutedLabel,
            Location = new Point(24, 14),
            AutoSize = true
        };

        // Line 1: Rental Fee
        lblRentalFeeTitle = new Label
        {
            Text = "Base Rental Fee",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(24, 38),
            AutoSize = true
        };

        lblRentalFeeAmount = new Label
        {
            Text = "₱0.00",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(card.Width - 180, 38),
            Size = new Size(156, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Line 2: Loyalty Perk / Discount
        lblDiscountTitle = new Label
        {
            Text = "Loyalty Perk",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(24, 62),
            AutoSize = true
        };

        lblDiscountAmount = new Label
        {
            Text = "₱0.00 (Standard Tier)",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(card.Width - 260, 62),
            Size = new Size(236, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Line 3: Security Deposit
        lblDepositTitle = new Label
        {
            Text = "Security Deposit Escrow",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = ColorSubtext,
            Location = new Point(24, 86),
            AutoSize = true
        };

        lblDepositAmount = new Label
        {
            Text = "₱0.00",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(card.Width - 180, 86),
            Size = new Size(156, 20),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Line 4: Total Due (Highlighted)
        lblTotalDueTitle = new Label
        {
            Text = "Total Due",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
            ForeColor = ColorPrimary,
            Location = new Point(24, 136),
            AutoSize = true
        };

        lblTotalDueAmount = new Label
        {
            Text = "₱0.00",
            UseMnemonic = false,
            Font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold),
            ForeColor = ColorAccent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(card.Width - 180, 134),
            Size = new Size(156, 26),
            TextAlign = ContentAlignment.MiddleRight
        };

        // Footnote
        lblDepositNote = new Label
        {
            Text = "Deposit is refundable upon return of garments in good condition.",
            UseMnemonic = false,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = ColorSubtext,
            Location = new Point(24, 168),
            AutoSize = true
        };

        card.Controls.AddRange(new Control[]
        {
            lblFeeTag,
            lblRentalFeeTitle, lblRentalFeeAmount,
            lblDiscountTitle, lblDiscountAmount,
            lblDepositTitle, lblDepositAmount,
            lblTotalDueTitle, lblTotalDueAmount,
            lblDepositNote
        });

        return card;
    }

    private void ListBoxPaymentMethods_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= listBoxPaymentMethods.Items.Count || e.Graphics == null)
            return;

        string methodName = listBoxPaymentMethods.Items[e.Index].ToString() ?? string.Empty;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bounds = e.Bounds;

        // Gap Fill
        using (var bgBrush = new SolidBrush(ColorViewBg))
        {
            g.FillRectangle(bgBrush, bounds);
        }

        // Floating Card Container
        var cardRect = new Rectangle(bounds.Left + 1, bounds.Top + 2, bounds.Width - 4, bounds.Height - 6);

        using (var cardPath = CreateRoundedRectangle(cardRect, 6))
        {
            using (var fillBrush = new SolidBrush(isSelected ? ColorActivePill : ColorCardBg))
            {
                g.FillPath(fillBrush, cardPath);
            }

            using (var borderPen = new Pen(isSelected ? ColorAccent : ColorBorder, isSelected ? 1.4f : 1f))
            {
                g.DrawPath(borderPen, cardPath);
            }
        }

        // Radio Button Indicator
        int radioDiameter = 14;
        int radioX = cardRect.Left + 18;
        int radioY = cardRect.Top + (cardRect.Height - radioDiameter) / 2;
        var radioRect = new Rectangle(radioX, radioY, radioDiameter, radioDiameter);

        using (var radioPen = new Pen(isSelected ? ColorAccent : ColorBorder, 1.5f))
        {
            g.DrawEllipse(radioPen, radioRect);
        }

        if (isSelected)
        {
            int dotDiameter = 6;
            int dotX = radioX + (radioDiameter - dotDiameter) / 2;
            int dotY = radioY + (radioDiameter - dotDiameter) / 2;
            using var dotBrush = new SolidBrush(ColorAccent);
            g.FillEllipse(dotBrush, new Rectangle(dotX, dotY, dotDiameter, dotDiameter));
        }

        // Method Text
        int textLeft = radioX + radioDiameter + 16;
        using var font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
        var textSize = TextRenderer.MeasureText(g, methodName, font);
        int textY = cardRect.Top + (cardRect.Height - textSize.Height) / 2;

        TextRenderer.DrawText(g, methodName, font, new Point(textLeft, textY), ColorPrimary);
    }

    // --- IBookingWizardStep Implementation ---

    public void OnStepEnter(BookingDraftModel draft)
    {
        _draft = draft;

        int itemCount = _draft.SelectedGarments.Count;
        decimal baseRentalFee = _draft.SubtotalRentalFee;
        decimal discountAmount = _draft.LoyaltyDiscountAmount;
        decimal deposit = _draft.TotalSecurityDeposit;
        decimal totalDue = _draft.TotalDue;

        lblRentalFeeTitle.Text = $"Base Rental Fee ({itemCount} item{(itemCount == 1 ? "" : "s")})";
        lblRentalFeeAmount.Text = $"{CurrencySymbol}{baseRentalFee:N2}";

        if (discountAmount > 0)
        {
            lblDiscountTitle.Text = $"Loyalty Perk ({_draft.LoyaltyTierName} – {_draft.LoyaltyDiscountPercentage:0.#}%)";
            lblDiscountTitle.ForeColor = ColorSuccess;
            lblDiscountAmount.Text = $"-{CurrencySymbol}{discountAmount:N2}";
            lblDiscountAmount.ForeColor = ColorSuccess;
        }
        else
        {
            lblDiscountTitle.Text = "Loyalty Perk";
            lblDiscountTitle.ForeColor = ColorSubtext;
            lblDiscountAmount.Text = "₱0.00 (Standard Tier)";
            lblDiscountAmount.ForeColor = ColorSubtext;
        }

        lblDepositAmount.Text = $"{CurrencySymbol}{deposit:N2}";
        lblTotalDueAmount.Text = $"{CurrencySymbol}{totalDue:N2}";
        lblDepositNote.Text = $"Refundable security deposit ({_draft.DepositPercentage:0.#}%) returned upon good condition.";

        if (!string.IsNullOrWhiteSpace(_draft.SelectedPaymentMethod))
        {
            int index = _paymentMethods.IndexOf(_draft.SelectedPaymentMethod);
            if (index >= 0)
            {
                listBoxPaymentMethods.SelectedIndex = index;
            }
        }
    }

    public void OnStepLeave(BookingDraftModel draft)
    {
        draft.SelectedPaymentMethod = _selectedPaymentMethod;
    }

    public bool ValidateStep(out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(_selectedPaymentMethod))
        {
            errorMessage = "Please choose a payment method to complete the booking.";
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

    private class PaymentCardListBox : ListBox
    {
        public PaymentCardListBox()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}