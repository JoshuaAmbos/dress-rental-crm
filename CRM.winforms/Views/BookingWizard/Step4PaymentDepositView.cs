using CRM.winforms.Models;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Views;

public partial class Step4PaymentDepositView : UserControl, IBookingWizardStep
{
    private BookingDraftModel? _draft;
    private string _selectedPaymentMethod = "Gcash";

    private readonly List<string> _paymentMethods = new()
    {
        "Gcash",
        "Credit Card – Visa",
        "Credit Card – Amex",
        "Bank Transfer / ACH",
        "Cash on Pickup",
        "Invoice / Net-30"
    };

    private Label lblRentalFeeTitle = null!;
    private Label lblRentalFeeAmount = null!;
    private Label lblDiscountTitle = null!;
    private Label lblDiscountAmount = null!;
    private Label lblDepositAmount = null!;
    private Label lblTotalDueAmount = null!;
    private Label lblDepositNote = null!;
    private PaymentCardListBox listBoxPaymentMethods = null!;

    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBorder = Color.FromArgb(234, 223, 217);
    private static readonly Color ColorDivider = Color.FromArgb(242, 235, 235);
    private static readonly Color ColorCardBg = Color.White;
    private static readonly Color ColorSelectedBg = Color.FromArgb(254, 242, 243);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorSuccess = Color.FromArgb(5, 150, 105);

    private const string CurrencySymbol = "₱";

    public string StepTitle => "Payment & Deposit";

    public Step4PaymentDepositView()
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
            Text = "Payment & Deposit",
            Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            Location = new Point(0, 0),
            AutoSize = true
        };
        pnlHeader.Controls.Add(lblTitle);

        var pnlHeaderSpacer = new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent };
        var pnlFeeCard = BuildFeeBreakdownCard();
        var pnlCardSpacer = new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };

        var lblMethodHeader = new Label
        {
            Text = "Payment Method",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = ColorSubtext,
            Dock = DockStyle.Top,
            Height = 26,
            TextAlign = ContentAlignment.BottomLeft
        };

        var pnlMethodSpacer = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

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
            if (listBoxPaymentMethods.SelectedItem is string m) _selectedPaymentMethod = m;
        };

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
            Height = 195,
            BackColor = ColorCardBg,
            Padding = new Padding(24, 16, 24, 16)
        };

        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var borderPen = new Pen(ColorBorder, 1.25f);
            e.Graphics.DrawPath(borderPen, borderPath);

            using var dividerPen = new Pen(ColorDivider, 1f);
            e.Graphics.DrawLine(dividerPen, 24, 126, card.Width - 24, 126);
        };

        var lblFeeTag = new Label
        {
            Text = "FEE BREAKDOWN",
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
            ForeColor = ColorSubtext,
            Location = new Point(24, 14),
            AutoSize = true
        };

        lblRentalFeeTitle = new Label { Text = "Base Rental Fee", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorSubtext, Location = new Point(24, 38), AutoSize = true };
        lblRentalFeeAmount = new Label { Text = "₱0", Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), ForeColor = ColorEspresso, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(card.Width - 160, 38), Size = new Size(136, 20), TextAlign = ContentAlignment.MiddleRight };

        lblDiscountTitle = new Label { Text = "Loyalty Discount", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorSuccess, Location = new Point(24, 62), AutoSize = true };
        lblDiscountAmount = new Label { Text = "₱0", Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), ForeColor = ColorSuccess, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(card.Width - 160, 62), Size = new Size(136, 20), TextAlign = ContentAlignment.MiddleRight };

        var lblDepositTitle = new Label { Text = "Security Deposit", Font = new Font("Segoe UI", 9.5f), ForeColor = ColorSubtext, Location = new Point(24, 86), AutoSize = true };
        lblDepositAmount = new Label { Text = "₱0", Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), ForeColor = ColorEspresso, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(card.Width - 160, 86), Size = new Size(136, 20), TextAlign = ContentAlignment.MiddleRight };

        var lblTotalDueTitle = new Label { Text = "Total Due", Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold), ForeColor = ColorEspresso, Location = new Point(24, 136), AutoSize = true };
        lblTotalDueAmount = new Label { Text = "₱0", Font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold), ForeColor = ColorDustyRose, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(card.Width - 160, 134), Size = new Size(136, 26), TextAlign = ContentAlignment.MiddleRight };

        lblDepositNote = new Label { Text = "Deposit of ₱0 is refundable upon return in good condition.", Font = new Font("Segoe UI", 8.5f), ForeColor = ColorSubtext, Location = new Point(24, 168), AutoSize = true };

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
        if (e.Index < 0 || e.Index >= listBoxPaymentMethods.Items.Count || e.Graphics == null) return;

        string methodName = listBoxPaymentMethods.Items[e.Index].ToString() ?? string.Empty;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bounds = e.Bounds;

        using (var bgBrush = new SolidBrush(ColorViewBg)) g.FillRectangle(bgBrush, bounds);

        var cardRect = new Rectangle(bounds.Left + 1, bounds.Top + 2, bounds.Width - 4, bounds.Height - 6);
        using (var cardPath = CreateRoundedRectangle(cardRect, 6))
        {
            using var fillBrush = new SolidBrush(isSelected ? ColorSelectedBg : ColorCardBg);
            using var borderPen = new Pen(isSelected ? ColorDustyRose : ColorBorder, isSelected ? 1.4f : 1f);
            g.FillPath(fillBrush, cardPath);
            g.DrawPath(borderPen, cardPath);
        }

        int radioDiameter = 14;
        int radioX = cardRect.Left + 18;
        int radioY = cardRect.Top + (cardRect.Height - radioDiameter) / 2;
        var radioRect = new Rectangle(radioX, radioY, radioDiameter, radioDiameter);

        using (var radioPen = new Pen(isSelected ? ColorDustyRose : ColorBorder, 1.5f))
            g.DrawEllipse(radioPen, radioRect);

        if (isSelected)
        {
            int dotDiameter = 6;
            int dotX = radioX + (radioDiameter - dotDiameter) / 2;
            int dotY = radioY + (radioDiameter - dotDiameter) / 2;
            using var dotBrush = new SolidBrush(ColorDustyRose);
            g.FillEllipse(dotBrush, new Rectangle(dotX, dotY, dotDiameter, dotDiameter));
        }

        int textLeft = radioX + radioDiameter + 16;
        using var font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold);
        var textSize = TextRenderer.MeasureText(g, methodName, font);
        int textY = cardRect.Top + (cardRect.Height - textSize.Height) / 2;
        TextRenderer.DrawText(g, methodName, font, new Point(textLeft, textY), ColorEspresso);
    }

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

        lblDiscountTitle.Text = $"Loyalty Perk ({_draft.LoyaltyTierName} – {_draft.LoyaltyDiscountPercentage:0.#}%)";
        lblDiscountAmount.Text = discountAmount > 0 ? $"-{CurrencySymbol}{discountAmount:N2}" : $"{CurrencySymbol}0.00";
        lblDiscountTitle.Visible = discountAmount > 0;
        lblDiscountAmount.Visible = discountAmount > 0;

        lblDepositAmount.Text = $"{CurrencySymbol}{deposit:N2}";
        lblTotalDueAmount.Text = $"{CurrencySymbol}{totalDue:N2}";
        lblDepositNote.Text = $"Deposit of {CurrencySymbol}{deposit:N2} is refundable upon return in good condition.";

        if (!string.IsNullOrWhiteSpace(_draft.SelectedPaymentMethod))
        {
            int index = _paymentMethods.IndexOf(_draft.SelectedPaymentMethod);
            if (index >= 0) listBoxPaymentMethods.SelectedIndex = index;
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