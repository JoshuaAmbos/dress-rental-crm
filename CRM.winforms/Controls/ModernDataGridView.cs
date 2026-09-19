using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class ModernDataGridView : DataGridView
{
    // Atelier Palette
    private static readonly Color ColorHeaderBg = Color.White;
    private static readonly Color ColorHeaderText = Color.FromArgb(130, 120, 125);
    private static readonly Color ColorGridLine = Color.FromArgb(242, 236, 238);
    private static readonly Color ColorSelectionBg = Color.FromArgb(190, 110, 120); // Dusty Rose
    private static readonly Color ColorTextPrimary = Color.FromArgb(44, 34, 38);   // Espresso
    private static readonly Color ColorSubtext = Color.FromArgb(150, 140, 145);
    private static readonly Color ColorBorder = Color.FromArgb(226, 218, 220);

    private Point _mouseDownCell = new(-1, -1);

    [Category("Appearance")]
    [DefaultValue("No records found.")]
    public string EmptyStateMessage { get; set; } = "No records found.";

    public ModernDataGridView()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        DoubleBuffered = true;

        // Structure & Grid Setup
        ReadOnly = true;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToOrderColumns = false;
        RowHeadersVisible = false;
        EnableHeadersVisualStyles = false;
        BackgroundColor = Color.White;
        BorderStyle = BorderStyle.None;

        // Disable built-in cell borders; we handle the clean bottom border manually
        CellBorderStyle = DataGridViewCellBorderStyle.None;
        GridColor = ColorGridLine;

        // Modern Dimensions & Typography
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 44;
        RowTemplate.Height = 42;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header Styling
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorHeaderBg,
            ForeColor = ColorHeaderText,
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            SelectionBackColor = ColorHeaderBg,
            SelectionForeColor = ColorHeaderText
        };

        // Standard Rows
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = ColorTextPrimary,
            SelectionBackColor = ColorSelectionBg,
            SelectionForeColor = Color.White,
            Padding = new Padding(12, 0, 12, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = ColorTextPrimary,
            SelectionBackColor = ColorSelectionBg,
            SelectionForeColor = Color.White,
            Padding = new Padding(12, 0, 12, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
    }

    protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
    {
        base.OnCellMouseDown(e);
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && Columns[e.ColumnIndex] is DataGridViewButtonColumn)
        {
            _mouseDownCell = new Point(e.ColumnIndex, e.RowIndex);
            InvalidateCell(e.ColumnIndex, e.RowIndex);
        }
    }

    protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
    {
        base.OnCellMouseUp(e);
        if (_mouseDownCell.X >= 0)
        {
            int col = _mouseDownCell.X;
            int row = _mouseDownCell.Y;
            _mouseDownCell = new Point(-1, -1);
            if (col < ColumnCount && row < RowCount)
            {
                InvalidateCell(col, row);
            }
        }
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null) return;

        // 1. Column Headers
        if (e.RowIndex == -1)
        {
            using (var bgBrush = new SolidBrush(ColorHeaderBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            // Draw crisp bottom dividing line under header
            using (var linePen = new Pen(ColorGridLine, 1.2f))
            {
                e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            var headerText = e.FormattedValue?.ToString() ?? string.Empty;
            var textRect = new Rectangle(
                e.CellBounds.X + 12,
                e.CellBounds.Y,
                e.CellBounds.Width - 24,
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                headerText,
                ColumnHeadersDefaultCellStyle.Font,
                textRect,
                ColorHeaderText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            e.Handled = true;
            return;
        }

        // 2. Data Rows
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            bool isSelected = (e.State & DataGridViewElementStates.Selected) != 0;
            Color backColor = isSelected ? ColorSelectionBg : Color.White;

            using (var bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.CellBounds);
            }

            // Bottom horizontal row divider line
            using (var linePen = new Pen(ColorGridLine, 1f))
            {
                e.Graphics.DrawLine(linePen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            // Custom Button Column
            if (Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                var cellValue = e.FormattedValue?.ToString() ?? string.Empty;
                var isPressed = _mouseDownCell.X == e.ColumnIndex && _mouseDownCell.Y == e.RowIndex;

                int btnHeight = 26;
                int btnMarginX = 6;
                int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                int btnWidth = e.CellBounds.Width - (btnMarginX * 2);

                if (btnWidth > 0 && btnHeight > 0)
                {
                    var btnRect = new Rectangle(e.CellBounds.X + btnMarginX, btnY, btnWidth, btnHeight);

                    Color btnBg;
                    Color btnFg;
                    Color btnBorderColor;

                    if (isSelected)
                    {
                        // Clean contrast on selected rows
                        btnBg = Color.White;
                        btnFg = ColorSelectionBg;
                        btnBorderColor = Color.White;

                        if (cellValue == "Restore") btnFg = Color.FromArgb(40, 120, 60);
                        if (cellValue == "Archive") btnFg = Color.FromArgb(175, 45, 55);
                    }
                    else
                    {
                        if (cellValue == "Restore")
                        {
                            btnBg = Color.FromArgb(240, 248, 242);
                            btnFg = Color.FromArgb(40, 125, 60);
                            btnBorderColor = Color.FromArgb(200, 230, 205);
                        }
                        else if (cellValue == "Archive")
                        {
                            btnBg = Color.FromArgb(253, 242, 242);
                            btnFg = Color.FromArgb(175, 45, 55);
                            btnBorderColor = Color.FromArgb(242, 212, 212);
                        }
                        else
                        {
                            btnBg = Color.FromArgb(248, 244, 245);
                            btnFg = ColorTextPrimary;
                            btnBorderColor = ColorBorder;
                        }
                    }

                    if (isPressed)
                    {
                        btnBg = Color.FromArgb(
                            Math.Max(0, btnBg.R - 20),
                            Math.Max(0, btnBg.G - 20),
                            Math.Max(0, btnBg.B - 20));
                    }

                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var path = CreateRoundedRect(btnRect, 5))
                    {
                        using var fillBrush = new SolidBrush(btnBg);
                        e.Graphics.FillPath(fillBrush, path);

                        using var borderPen = new Pen(btnBorderColor, 1f);
                        e.Graphics.DrawPath(borderPen, path);
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        cellValue,
                        new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                        btnRect,
                        btnFg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
                }

                e.Handled = true;
                return;
            }

            // Standard Text Cells
            Color textColor = isSelected ? Color.White : ColorTextPrimary;
            var cellText = e.FormattedValue?.ToString() ?? string.Empty;

            var textRect = new Rectangle(
                e.CellBounds.X + 12,
                e.CellBounds.Y,
                e.CellBounds.Width - 24,
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                cellText,
                this.Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis);

            e.Handled = true;
            return;
        }

        base.OnCellPainting(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (RowCount == 0 && !DesignMode)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var emptyRect = new Rectangle(0, ColumnHeadersHeight, Width, Height - ColumnHeadersHeight);
            using var brush = new SolidBrush(Color.White);
            g.FillRectangle(brush, emptyRect);

            using var titleFont = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9.5f);

            var titleRect = new Rectangle(0, emptyRect.Top + (emptyRect.Height / 2) - 28, Width, 28);
            var subRect = new Rectangle(0, titleRect.Bottom, Width, 24);

            TextRenderer.DrawText(
                g,
                EmptyStateMessage,
                titleFont,
                titleRect,
                ColorTextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(
                g,
                "Try adjusting your search filters or create a new record.",
                subFont,
                subRect,
                ColorSubtext,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}