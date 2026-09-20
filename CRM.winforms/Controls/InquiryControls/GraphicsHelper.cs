using System.Drawing.Drawing2D;

namespace CRM.winforms.Controls;

public static class GraphicsHelper
{
    public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
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

    public static void ApplyRoundedCard(Panel card, Color borderColor, int radius = 8, float borderWidth = 1.25f)
    {
        card.Resize += (s, e) =>
        {
            if (card.Width <= radius * 2 || card.Height <= radius * 2) return;
            var oldRegion = card.Region;
            using var clipPath = CreateRoundedRectangle(new Rectangle(0, 0, card.Width, card.Height), radius);
            card.Region = new Region(clipPath);
            oldRegion?.Dispose();
        };

        card.Paint += (s, e) =>
        {
            if (card.Width <= radius * 2 || card.Height <= radius * 2) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(borderColor, borderWidth);
            using var path = CreateRoundedRectangle(new Rectangle(0, 0, card.Width - 1, card.Height - 1), radius);
            e.Graphics.DrawPath(pen, path);
        };
    }
}