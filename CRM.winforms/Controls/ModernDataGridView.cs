using System.Drawing;
using System.Windows.Forms;

namespace CRM.winforms.Controls;

public class ModernDataGridView : DataGridView
{
    private static readonly Color HeaderBackground = Color.FromArgb(244, 238, 238);   // Soft Rose Tint
    private static readonly Color HeaderText = Color.FromArgb(38, 22, 24);             // Deep Espresso
    private static readonly Color GridLineClr = Color.FromArgb(228, 222, 222);         // Soft Grid Divider
    private static readonly Color AlternatingRowClr = Color.FromArgb(253, 250, 250);   // Subtle Zebra Stripe
    private static readonly Color SelectionBg = Color.FromArgb(190, 110, 120);         // Brand Dusty Rose
    private static readonly Color SelectionFg = Color.White;

    public ModernDataGridView()
    {
        // Double buffering prevents flickering during fast scrolling/resizing
        this.DoubleBuffered = true;

        // Desktop Table Ergonomics
        this.BackgroundColor = Color.White;
        this.BorderStyle = BorderStyle.FixedSingle;
        this.GridColor = GridLineClr;
        this.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        this.RowHeadersVisible = false;
        this.MultiSelect = false;
        this.ReadOnly = true;
        this.AllowUserToAddRows = false;
        this.AllowUserToDeleteRows = false;
        this.AllowUserToResizeRows = false;
        this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Row Heights & Sizing
        this.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        this.ColumnHeadersHeight = 36;
        this.RowTemplate.Height = 32;
        this.EnableHeadersVisualStyles = false;

        // Typography & Palette
        this.Font = new Font("Segoe UI", 9.5f);

        // Header Styling
        this.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = HeaderBackground,
            ForeColor = HeaderText,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            SelectionBackColor = HeaderBackground,
            SelectionForeColor = HeaderText
        };

        // Standard Cell Styling
        this.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(45, 35, 40),
            SelectionBackColor = SelectionBg,
            SelectionForeColor = SelectionFg,
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // Subtle Alternating Striping
        this.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AlternatingRowClr,
            ForeColor = Color.FromArgb(45, 35, 40),
            SelectionBackColor = SelectionBg,
            SelectionForeColor = SelectionFg,
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
    }
}