namespace CRM.winforms.Views;

partial class CustomerProfilesView
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
        components = new System.ComponentModel.Container();
        label1 = new Label();
        primaryButtonNewCustomer = new CRM.winforms.Controls.PrimaryButton();
        chkShowArchived = new CRM.winforms.Controls.AtelierCheckBox();
        searchBar1 = new CRM.winforms.Controls.SearchBar();
        pnlGridCard = new Panel();
        dgvCustomers = new DataGridView();
        pnlGridCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
        SuspendLayout();

        // 
        // label1 (Page Title)
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(32, 24);
        label1.Name = "label1";
        label1.Size = new Size(198, 32);
        label1.TabIndex = 0;
        label1.Text = "Client Directory";

        // 
        // searchBar1 (Search Input Box)
        // 
        searchBar1.BackColor = Color.Transparent;
        searchBar1.Location = new Point(32, 78);
        searchBar1.Name = "searchBar1";
        searchBar1.Size = new Size(340, 36);
        searchBar1.TabIndex = 1;

        // 
        // chkShowArchived (Archived Filter Toggle)
        // 
        chkShowArchived.BackColor = Color.Transparent;
        chkShowArchived.Font = new Font("Segoe UI", 9.25F);
        chkShowArchived.ForeColor = Color.FromArgb(115, 105, 110);
        chkShowArchived.Location = new Point(386, 82);
        chkShowArchived.Name = "chkShowArchived";
        chkShowArchived.Size = new Size(135, 28);
        chkShowArchived.TabIndex = 2;
        chkShowArchived.Text = "Show Archived";
        chkShowArchived.UseVisualStyleBackColor = false;

        // 
        // primaryButtonNewCustomer (Primary Action)
        // 
        primaryButtonNewCustomer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        primaryButtonNewCustomer.BackColor = Color.FromArgb(186, 105, 115);
        primaryButtonNewCustomer.FlatAppearance.BorderSize = 0;
        primaryButtonNewCustomer.FlatStyle = FlatStyle.Flat;
        primaryButtonNewCustomer.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        primaryButtonNewCustomer.ForeColor = Color.White;
        primaryButtonNewCustomer.Location = new Point(1024, 76);
        primaryButtonNewCustomer.Name = "primaryButtonNewCustomer";
        primaryButtonNewCustomer.Size = new Size(144, 38);
        primaryButtonNewCustomer.TabIndex = 3;
        primaryButtonNewCustomer.Text = "+ New Client";
        primaryButtonNewCustomer.UseVisualStyleBackColor = false;

        // 
        // pnlGridCard (White Card Wrapper with 1px Border)
        // 
        pnlGridCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pnlGridCard.BackColor = Color.White;
        pnlGridCard.Controls.Add(dgvCustomers);
        pnlGridCard.Location = new Point(32, 128);
        pnlGridCard.Name = "pnlGridCard";
        pnlGridCard.Padding = new Padding(1);
        pnlGridCard.Size = new Size(1136, 608);
        pnlGridCard.TabIndex = 4;
        pnlGridCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(234, 223, 217), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1);
        };

        // 
        // dgvCustomers (Atelier Grid)
        // 
        dgvCustomers.AllowUserToAddRows = false;
        dgvCustomers.AllowUserToDeleteRows = false;
        dgvCustomers.AllowUserToResizeRows = false;
        dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCustomers.BackgroundColor = Color.White;
        dgvCustomers.BorderStyle = BorderStyle.None;
        dgvCustomers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvCustomers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvCustomers.Dock = DockStyle.Fill;
        dgvCustomers.GridColor = Color.FromArgb(242, 235, 235);
        dgvCustomers.Location = new Point(1, 1);
        dgvCustomers.MultiSelect = false;
        dgvCustomers.Name = "dgvCustomers";
        dgvCustomers.ReadOnly = true;
        dgvCustomers.RowHeadersVisible = false;
        dgvCustomers.RowTemplate.Height = 42;
        dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCustomers.Size = new Size(1134, 606);
        dgvCustomers.TabIndex = 5;

        // 
        // CustomerProfilesView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(pnlGridCard);
        Controls.Add(primaryButtonNewCustomer);
        Controls.Add(chkShowArchived);
        Controls.Add(searchBar1);
        Controls.Add(label1);
        Name = "CustomerProfilesView";
        Padding = new Padding(32, 24, 32, 24);
        Size = new Size(1200, 760);
        Load += CustomerProfilesView_Load;
        pnlGridCard.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Controls.PrimaryButton primaryButtonNewCustomer;
    private Controls.AtelierCheckBox chkShowArchived;
    private Controls.SearchBar searchBar1;
    private Panel pnlGridCard;
    private DataGridView dgvCustomers;
}