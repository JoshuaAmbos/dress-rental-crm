using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace CRM.winforms.Views;

public partial class BookingSuccessView : UserControl
{
    private readonly string _bookingCode;
    private readonly Action _onReturnToDashboard;

    // Atelier Palette
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorDustyRose = Color.FromArgb(186, 105, 115);
    private static readonly Color ColorSubtext = Color.FromArgb(145, 135, 140);
    private static readonly Color ColorBadgeBg = Color.FromArgb(232, 248, 238);
    private static readonly Color ColorCheckmark = Color.FromArgb(34, 139, 64);
    private static readonly Color ColorViewBg = Color.FromArgb(249, 241, 241);

    public BookingSuccessView(string bookingCode, Action onReturnToDashboard)
    {
        _bookingCode = string.IsNullOrWhiteSpace(bookingCode) ? "BKG-0000" : bookingCode;
        _onReturnToDashboard = onReturnToDashboard ?? throw new ArgumentNullException(nameof(onReturnToDashboard));

        BuildLayout();
    }

    private void BuildLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = ColorViewBg;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        var pnlCenter = new Panel
        {
            Size = new Size(500, 320),
            BackColor = Color.Transparent
        };

        Resize += (s, e) =>
        {
            pnlCenter.Location = new Point(
                Math.Max(0, (ClientSize.Width - pnlCenter.Width) / 2),
                Math.Max(0, (ClientSize.Height - pnlCenter.Height) / 2 - 20));
        };

        var pnlBadge = new Panel
        {
            Size = new Size(54, 54),
            Location = new Point((pnlCenter.Width - 54) / 2, 10),
            BackColor = Color.Transparent
        };
        pnlBadge.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var fillBrush = new SolidBrush(ColorBadgeBg);
            g.FillEllipse(fillBrush, 0, 0, 53, 53);

            using var pen = new Pen(ColorCheckmark, 2.75f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            g.DrawLines(pen, new[]
            {
                new Point(16, 28),
                new Point(23, 35),
                new Point(37, 18)
            });
        };

        var lblTitle = new Label
        {
            Text = "Booking Confirmed!",
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = ColorEspresso,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(pnlCenter.Width, 34),
            Location = new Point(0, 78)
        };

        var pnlText = new Panel
        {
            Size = new Size(pnlCenter.Width, 54),
            Location = new Point(0, 118),
            BackColor = Color.Transparent
        };
        pnlText.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using var regularFont = new Font("Segoe UI", 10f, FontStyle.Regular);
            using var codeFont = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);

            string part1 = "Booking ";
            string part2 = _bookingCode;
            string part3 = " has been created successfully.";

            var sz1 = TextRenderer.MeasureText(g, part1, regularFont);
            var sz2 = TextRenderer.MeasureText(g, part2, codeFont);
            var sz3 = TextRenderer.MeasureText(g, part3, regularFont);

            int totalLineWidth = sz1.Width + sz2.Width + sz3.Width - 14;
            int startX = (pnlText.Width - totalLineWidth) / 2;

            TextRenderer.DrawText(g, part1, regularFont, new Point(startX, 4), ColorSubtext);
            TextRenderer.DrawText(g, part2, codeFont, new Point(startX + sz1.Width - 7, 4), ColorDustyRose);
            TextRenderer.DrawText(g, part3, regularFont, new Point(startX + sz1.Width + sz2.Width - 14, 4), ColorSubtext);

            string line2 = "A confirmation has been sent to the client.";
            var szLine2 = TextRenderer.MeasureText(g, line2, regularFont);
            int startX2 = (pnlText.Width - szLine2.Width) / 2;
            TextRenderer.DrawText(g, line2, regularFont, new Point(startX2, 28), ColorSubtext);
        };

        var btnReturn = new Button
        {
            Text = "Return to Dashboard",
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = ColorDustyRose,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(184, 42),
            Location = new Point((pnlCenter.Width - 184) / 2, 196),
            Cursor = Cursors.Hand
        };
        btnReturn.FlatAppearance.BorderSize = 0;
        btnReturn.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, btnReturn.Width - 1, btnReturn.Height - 1), 7);
            btnReturn.Region = new Region(path);
        };
        btnReturn.Click += (s, e) => _onReturnToDashboard.Invoke();

        pnlCenter.Controls.Add(pnlBadge);
        pnlCenter.Controls.Add(lblTitle);
        pnlCenter.Controls.Add(pnlText);
        pnlCenter.Controls.Add(btnReturn);

        Controls.Add(pnlCenter);
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