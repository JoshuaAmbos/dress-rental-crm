namespace CRM.winforms.Views;

partial class CatalogView
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        label1 = new Label();
        lblSubtitle = new Label();
        pnlToolbar = new Panel();
        pnlFilterTabs = new FlowLayoutPanel();
        searchBar1 = new CRM.winforms.Controls.SearchBar();
        flpGarments = new FlowLayoutPanel();
        pnlToolbar.SuspendLayout();
        SuspendLayout();

        // 
        // label1 (Page Title)
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(32, 24);
        label1.Name = "label1";
        label1.Size = new Size(210, 32);
        label1.TabIndex = 0;
        label1.Text = "Garment Catalog";

        // 
        // lblSubtitle (Descriptive Tagline)
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 9.5F);
        lblSubtitle.ForeColor = Color.FromArgb(145, 135, 140);
        lblSubtitle.Location = new Point(34, 58);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(475, 17);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Browse wardrobe collection, check rental availability, and update garment status.";

        // 
        // pnlToolbar (Filter Strip & Search Bar Container)
        // 
        pnlToolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pnlToolbar.BackColor = Color.Transparent;
        pnlToolbar.Controls.Add(pnlFilterTabs);
        pnlToolbar.Controls.Add(searchBar1);
        pnlToolbar.Location = new Point(32, 88);
        pnlToolbar.Name = "pnlToolbar";
        pnlToolbar.Size = new Size(1136, 40);
        pnlToolbar.TabIndex = 2;

        // 
        // pnlFilterTabs (Status Filters Flow Container)
        // 
        pnlFilterTabs.Dock = DockStyle.Fill;
        pnlFilterTabs.Location = new Point(0, 0);
        pnlFilterTabs.Name = "pnlFilterTabs";
        pnlFilterTabs.Size = new Size(806, 40);
        pnlFilterTabs.TabIndex = 0;
        pnlFilterTabs.WrapContents = false;

        // 
        // searchBar1 (Search Input on the right)
        // 
        searchBar1.BackColor = Color.Transparent;
        searchBar1.Dock = DockStyle.Right;
        searchBar1.Location = new Point(806, 0);
        searchBar1.Name = "searchBar1";
        searchBar1.Size = new Size(330, 40);
        searchBar1.TabIndex = 1;

        // 
        // flpGarments (Dynamic Garment Cards Container)
        // 
        flpGarments.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        flpGarments.AutoScroll = true;
        flpGarments.BackColor = Color.Transparent;
        flpGarments.Location = new Point(24, 136);
        flpGarments.Name = "flpGarments";
        flpGarments.Padding = new Padding(8);
        flpGarments.Size = new Size(1152, 600);
        flpGarments.TabIndex = 3;

        // 
        // CatalogView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(flpGarments);
        Controls.Add(pnlToolbar);
        Controls.Add(lblSubtitle);
        Controls.Add(label1);
        Name = "CatalogView";
        Padding = new Padding(32, 24, 32, 24);
        Size = new Size(1200, 760);
        pnlToolbar.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Label lblSubtitle;
    private Panel pnlToolbar;
    private FlowLayoutPanel pnlFilterTabs;
    private Controls.SearchBar searchBar1;
    private FlowLayoutPanel flpGarments;
}