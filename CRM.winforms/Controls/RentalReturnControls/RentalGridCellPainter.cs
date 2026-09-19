using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CRM.winforms.Models.RentalBookingModels;

namespace CRM.winforms.Controls.RentalReturnControls;

public static class RentalGridCellPainter
{
    private static readonly Color ColorEspresso = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorTextRegular = Color.FromArgb(44, 34, 38);
    private static readonly Color ColorOverdueText = Color.FromArgb(180, 40, 50);

    public static void PaintCell(DataGridViewCellPaintingEventArgs e, BookingRowViewModel item, string columnName)
    {
        if (e.Graphics == null) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool isSelected = (e.State & DataGridViewElementStates.Selected) != 0;

        // Custom Paint: RENTAL PERIOD
        if (columnName is "rentalPeriodDataGridViewTextBoxColumn" or "ColRentalPeriod")
        {
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);

            string startStr = item.StartDate.ToString("MMM dd, yyyy");
            string arrow = "  →  ";
            string endStr = item.EndDate.ToString("MMM dd, yyyy");

            using var font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            using var fontBold = new Font("Segoe UI Semibold", 9.25f, FontStyle.Bold);

            Color textColor = isSelected ? Color.White : (item.IsOverdue ? ColorOverdueText : ColorTextRegular);
            Color arrowColor = isSelected ? Color.FromArgb(245, 225, 230) : Color.FromArgb(170, 160, 165);

            int y = e.CellBounds.Top + (e.CellBounds.Height - 18) / 2;
            int x = e.CellBounds.Left + 12;

            TextRenderer.DrawText(g, startStr, font, new Point(x, y), textColor);
            x += TextRenderer.MeasureText(g, startStr, font).Width;

            TextRenderer.DrawText(g, arrow, font, new Point(x, y), arrowColor);
            x += TextRenderer.MeasureText(g, arrow, font).Width;

            TextRenderer.DrawText(g, endStr, item.IsOverdue ? fontBold : font, new Point(x, y), textColor);
            e.Handled = true;
        }
        // Custom Paint: FEE / DEPOSIT
        else if (columnName is "feeDepositDataGridViewTextBoxColumn" or "ColFeeDeposit")
        {
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);

            string feeStr = $"₱{item.RentalFee:N0}";
            string depStr = $"dep. ₱{item.SecurityDeposit:N0}";

            using var feeFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            using var depFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);

            Color feeColor = isSelected ? Color.White : ColorEspresso;
            Color depColor = isSelected ? Color.FromArgb(245, 225, 230) : Color.FromArgb(120, 110, 115);

            int startY = e.CellBounds.Top + (e.CellBounds.Height - 32) / 2;
            int x = e.CellBounds.Left + 12;

            TextRenderer.DrawText(g, feeStr, feeFont, new Point(x, startY), feeColor);
            TextRenderer.DrawText(g, depStr, depFont, new Point(x, startY + 16), depColor);
            e.Handled = true;
        }
        // Custom Paint: STAGE
        else if (columnName is "stageDataGridViewTextBoxColumn" or "ColStage")
        {
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);

            string stage = item.IsOverdue ? "Overdue" : item.Stage;
            Color bg;
            Color fg;

            switch (stage.ToLowerInvariant())
            {
                case "active":
                    bg = Color.FromArgb(238, 244, 254);
                    fg = Color.FromArgb(41, 105, 209);
                    break;
                case "overdue":
                    bg = Color.FromArgb(254, 240, 242);
                    fg = Color.FromArgb(190, 40, 55);
                    break;
                case "reserved":
                case "fitting":
                    bg = Color.FromArgb(248, 242, 255);
                    fg = Color.FromArgb(135, 75, 200);
                    break;
                case "returned":
                    bg = Color.FromArgb(236, 248, 241);
                    fg = Color.FromArgb(38, 140, 80);
                    break;
                default:
                    bg = Color.FromArgb(245, 245, 245);
                    fg = Color.FromArgb(120, 110, 115);
                    break;
            }

            using var badgeFont = new Font("Segoe UI Semibold", 8f, FontStyle.Bold);
            int badgeW = TextRenderer.MeasureText(stage, badgeFont).Width + 14;
            int badgeH = 22;
            int bx = e.CellBounds.Left + 12;
            int by = e.CellBounds.Top + (e.CellBounds.Height - badgeH) / 2;
            var badgeRect = new Rectangle(bx, by, badgeW, badgeH);

            using (var path = CreateRoundedRectangle(badgeRect, 4))
            using (var brush = new SolidBrush(bg))
            {
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(g, stage, badgeFont, badgeRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            e.Handled = true;
        }
        // Custom Paint: ACTIONS BUTTON
        else if (columnName is "actionDataGridViewTextBoxColumn" or "ColAction")
        {
            e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground | DataGridViewPaintParts.Border);

            string stage = item.IsOverdue ? "Overdue" : item.Stage;
            string btnText = (stage is "Active" or "Overdue") ? "Return" : (stage == "Returned" ? "Settled" : "View");

            int btnWidth = 72;
            int btnHeight = 28;
            int bx = e.CellBounds.Left + 12;
            int by = e.CellBounds.Top + (e.CellBounds.Height - btnHeight) / 2;
            var actionRect = new Rectangle(bx, by, btnWidth, btnHeight);

            using (var path = CreateRoundedRectangle(actionRect, 4))
            using (var bgBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.FromArgb(210, 200, 200), 1f))
            {
                g.FillPath(bgBrush, path);
                g.DrawPath(borderPen, path);

                using var font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
                Color btnColor = (btnText == "Return") ? Color.FromArgb(170, 50, 60) : ColorEspresso;
                TextRenderer.DrawText(g, btnText, font, actionRect, btnColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            e.Handled = true;
        }
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