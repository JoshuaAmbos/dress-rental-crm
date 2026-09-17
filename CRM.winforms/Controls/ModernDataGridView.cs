using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class ModernDataGridView : DataGridView
{
    // Palette
    private static readonly Color ColorHeaderBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorHeaderText = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorGridLine = Color.FromArgb(238, 230, 230);
    private static readonly Color ColorRowHover = Color.FromArgb(253, 248, 248);
    private static readonly Color ColorAltRow = Color.FromArgb(254, 252, 252);
    private static readonly Color ColorSelectionBg = Color.FromArgb(190, 110, 120);
    private static readonly Color ColorTextPrimary = Color.FromArgb(44, 34, 38);
    private static readonly Color ColorSubtext = Color.FromArgb(120, 110, 115);

    // Interaction State
    private int _hoveredRowIndex = -1;
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

        // Grid Structure
        BackgroundColor = Color.White;
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        GridColor = ColorGridLine;
        RowHeadersVisible = false;
        MultiSelect = false;
        ReadOnly = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        AllowUserToOrderColumns = false;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        EnableHeadersVisualStyles = false;

        // Dimensions & Fonts
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 42;
        RowTemplate.Height = 38;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // Header Styling
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorHeaderBg,
            ForeColor = ColorHeaderText,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 14, 0),
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
            Padding = new Padding(14, 0, 14, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // Alternating Rows
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = ColorAltRow,
            ForeColor = ColorTextPrimary,
            SelectionBackColor = ColorSelectionBg,
            SelectionForeColor = Color.White,
            Padding = new Padding(14, 0, 14, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
    }

    // Row Hover Effects
    protected override void OnCellMouseEnter(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseEnter(e);
        if (e.RowIndex >= 0 && e.RowIndex != _hoveredRowIndex)
        {
            int previous = _hoveredRowIndex;
            _hoveredRowIndex = e.RowIndex;
            if (previous >= 0 && previous < RowCount) InvalidateRow(previous);
            InvalidateRow(_hoveredRowIndex);
        }
    }

    protected override void OnCellMouseLeave(DataGridViewCellEventArgs e)
    {
        base.OnCellMouseLeave(e);
        if (_hoveredRowIndex >= 0)
        {
            int previous = _hoveredRowIndex;
            _hoveredRowIndex = -1;
            if (previous < RowCount) InvalidateRow(previous);
        }
    }

    // Button Click State Tracking
    protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
    {
        base.OnCellMouseDown(e);
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            _mouseDownCell = new Point(e.ColumnIndex, e.RowIndex);
            if (Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
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
            if (col < ColumnCount && row < RowCount && Columns[col] is DataGridViewButtonColumn)
            {
                InvalidateCell(col, row);
            }
        }
    }

    // Custom Rendering
    protected override void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)
    {
        base.OnRowPrePaint(e);

        var row = Rows[e.RowIndex];
        if (!row.Selected && e.RowIndex == _hoveredRowIndex)
        {
            using var hoverBrush = new SolidBrush(ColorRowHover);
            e.Graphics.FillRectangle(hoverBrush, e.RowBounds);
        }
    }

    // Custom Rendering
    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && Columns[e.ColumnIndex] is DataGridViewButtonColumn)
        {
            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            var cellValue = e.FormattedValue?.ToString() ?? string.Empty;
            var isSelected = (e.State & DataGridViewElementStates.Selected) != 0;
            var isPressed = _mouseDownCell.X == e.ColumnIndex && _mouseDownCell.Y == e.RowIndex;

            var btnRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 5, e.CellBounds.Width - 12, e.CellBounds.Height - 10);
            if (btnRect.Width <= 0 || btnRect.Height <= 0) return;

            // Safe fallback to prevent CS8602 on nullable CellStyle
            Color btnBg = e.CellStyle?.BackColor ?? Color.FromArgb(244, 238, 238);
            Color btnFg = e.CellStyle?.ForeColor ?? Color.FromArgb(38, 22, 24);

            if (isSelected)
            {
                btnBg = Color.White;
                btnFg = ColorSelectionBg;
            }

            if (isPressed)
            {
                btnBg = Color.FromArgb(
                    Math.Max(0, btnBg.R - 20),
                    Math.Max(0, btnBg.G - 20),
                    Math.Max(0, btnBg.B - 20));
            }

            if (e.Graphics == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = CreateRoundedRect(btnRect, 5))
            {
                using var fillBrush = new SolidBrush(btnBg);
                e.Graphics.FillPath(fillBrush, path);

                using var borderPen = new Pen(Color.FromArgb(220, 210, 212), 1f);
                e.Graphics.DrawPath(borderPen, path);
            }

            TextRenderer.DrawText(
                e.Graphics,
                cellValue,
                new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold),
                btnRect,
                btnFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            e.Handled = true;
            return;
        }

        base.OnCellPainting(e);
    }

    // Empty State Drawing
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

            using var titleFont = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9f);

            var titleRect = new Rectangle(0, emptyRect.Top + (emptyRect.Height / 2) - 22, Width, 26);
            var subRect = new Rectangle(0, titleRect.Bottom, Width, 20);

            TextRenderer.DrawText(
                g,
                EmptyStateMessage,
                titleFont,
                titleRect,
                ColorHeaderText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(
                g,
                "Try adjusting your search criteria or add a new record.",
                subFont,
                subRect,
                ColorSubtext,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // Geometry Helpers
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