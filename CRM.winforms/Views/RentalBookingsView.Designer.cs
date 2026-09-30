namespace CRM.winforms.Views;

partial class RentalBookingsView
{
    private System.ComponentModel.IContainer components = null!;

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
        primaryButtonNewBooking = new CRM.winforms.Controls.PrimaryButton();
        tblKpis = new TableLayoutPanel();
        kpiCardControlActiveLeases = new CRM.winforms.Controls.KpiCardControl();
        kpiCardControlOverdue = new CRM.winforms.Controls.KpiCardControl();
        kpiCardControlUpcomingReturns = new CRM.winforms.Controls.KpiCardControl();
        pnlFilterStrip = new Panel();
        pnlFilterTabs = new FlowLayoutPanel();
        pnlSearch = new Panel();
        txtSearch = new TextBox();
        pnlGridCard = new Panel();
        dgvBookings = new DataGridView();
        bookingCodeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        clientNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        garmentSummaryDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        rentalPeriodDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        feeDepositDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        stageDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        actionDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        tblKpis.SuspendLayout();
        pnlFilterStrip.SuspendLayout();
        pnlSearch.SuspendLayout();
        pnlGridCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvBookings).BeginInit();
        SuspendLayout();

        // 
        // label1 (Page Title)
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(32, 24);
        label1.Name = "label1";
        label1.Size = new Size(328, 32);
        label1.TabIndex = 0;
        label1.Text = "Rental & Returns Pipeline";

        // 
        // lblSubtitle
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 9.5F);
        lblSubtitle.ForeColor = Color.FromArgb(145, 135, 140);
        lblSubtitle.Location = new Point(34, 58);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(392, 17);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Monitor reservations, ongoing fittings, active leases, and returns.";

        // 
        // primaryButtonNewBooking
        // 
        primaryButtonNewBooking.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        primaryButtonNewBooking.BackColor = Color.FromArgb(186, 105, 115);
        primaryButtonNewBooking.FlatAppearance.BorderSize = 0;
        primaryButtonNewBooking.FlatStyle = FlatStyle.Flat;
        primaryButtonNewBooking.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        primaryButtonNewBooking.ForeColor = Color.White;
        primaryButtonNewBooking.Location = new Point(1024, 28);
        primaryButtonNewBooking.Name = "primaryButtonNewBooking";
        primaryButtonNewBooking.Size = new Size(144, 38);
        primaryButtonNewBooking.TabIndex = 2;
        primaryButtonNewBooking.Text = "+ New Booking";
        primaryButtonNewBooking.UseVisualStyleBackColor = false;
        primaryButtonNewBooking.Click += PrimaryButtonNewBooking_Click;

        // 
        // tblKpis (Responsive 3-Column Strip)
        // 
        tblKpis.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        tblKpis.ColumnCount = 3;
        tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        tblKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        tblKpis.Controls.Add(kpiCardControlActiveLeases, 0, 0);
        tblKpis.Controls.Add(kpiCardControlOverdue, 1, 0);
        tblKpis.Controls.Add(kpiCardControlUpcomingReturns, 2, 0);
        tblKpis.Location = new Point(32, 92);
        tblKpis.Name = "tblKpis";
        tblKpis.RowCount = 1;
        tblKpis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tblKpis.Size = new Size(1136, 120);
        tblKpis.TabIndex = 3;

        // 
        // kpiCardControlActiveLeases
        // 
        kpiCardControlActiveLeases.BackColor = Color.White;
        kpiCardControlActiveLeases.Dock = DockStyle.Fill;
        kpiCardControlActiveLeases.Location = new Point(0, 0);
        kpiCardControlActiveLeases.Margin = new Padding(0, 0, 10, 0);
        kpiCardControlActiveLeases.Name = "kpiCardControlActiveLeases";
        kpiCardControlActiveLeases.Size = new Size(368, 120);
        kpiCardControlActiveLeases.TabIndex = 0;

        // 
        // kpiCardControlOverdue
        // 
        kpiCardControlOverdue.BackColor = Color.White;
        kpiCardControlOverdue.Dock = DockStyle.Fill;
        kpiCardControlOverdue.Location = new Point(383, 0);
        kpiCardControlOverdue.Margin = new Padding(5, 0, 5, 0);
        kpiCardControlOverdue.Name = "kpiCardControlOverdue";
        kpiCardControlOverdue.Size = new Size(368, 120);
        kpiCardControlOverdue.TabIndex = 1;

        // 
        // kpiCardControlUpcomingReturns
        // 
        kpiCardControlUpcomingReturns.BackColor = Color.White;
        kpiCardControlUpcomingReturns.Dock = DockStyle.Fill;
        kpiCardControlUpcomingReturns.Location = new Point(766, 0);
        kpiCardControlUpcomingReturns.Margin = new Padding(10, 0, 0, 0);
        kpiCardControlUpcomingReturns.Name = "kpiCardControlUpcomingReturns";
        kpiCardControlUpcomingReturns.Size = new Size(370, 120);
        kpiCardControlUpcomingReturns.TabIndex = 2;

        // 
        // pnlFilterStrip
        // 
        pnlFilterStrip.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        pnlFilterStrip.Controls.Add(pnlFilterTabs);
        pnlFilterStrip.Controls.Add(pnlSearch);
        pnlFilterStrip.Location = new Point(32, 226);
        pnlFilterStrip.Name = "pnlFilterStrip";
        pnlFilterStrip.Size = new Size(1136, 38);
        pnlFilterStrip.TabIndex = 4;

        // 
        // pnlFilterTabs
        // 
        pnlFilterTabs.Dock = DockStyle.Fill;
        pnlFilterTabs.Location = new Point(0, 0);
        pnlFilterTabs.Name = "pnlFilterTabs";
        pnlFilterTabs.Size = new Size(866, 38);
        pnlFilterTabs.TabIndex = 0;
        pnlFilterTabs.WrapContents = false;

        // 
        // pnlSearch
        // 
        pnlSearch.BackColor = Color.White;
        pnlSearch.Controls.Add(txtSearch);
        pnlSearch.Dock = DockStyle.Right;
        pnlSearch.Location = new Point(866, 0);
        pnlSearch.Name = "pnlSearch";
        pnlSearch.Size = new Size(270, 38);
        pnlSearch.TabIndex = 1;
        pnlSearch.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(234, 223, 217), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
        };

        // 
        // txtSearch
        // 
        txtSearch.BorderStyle = BorderStyle.None;
        txtSearch.Font = new Font("Segoe UI", 9.5F);
        txtSearch.Location = new Point(12, 10);
        txtSearch.Name = "txtSearch";
        txtSearch.Size = new Size(246, 17);
        txtSearch.TabIndex = 0;

        // 
        // pnlGridCard (White Container Card with 1px Border)
        // 
        pnlGridCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pnlGridCard.BackColor = Color.White;
        pnlGridCard.Controls.Add(dgvBookings);
        pnlGridCard.Location = new Point(32, 276);
        pnlGridCard.Name = "pnlGridCard";
        pnlGridCard.Padding = new Padding(1);
        pnlGridCard.Size = new Size(1136, 460);
        pnlGridCard.TabIndex = 5;
        pnlGridCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(234, 223, 217), 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1);
        };

        // 
        // dgvBookings
        // 
        dgvBookings.AllowUserToAddRows = false;
        dgvBookings.AllowUserToDeleteRows = false;
        dgvBookings.AllowUserToResizeRows = false;
        dgvBookings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvBookings.BackgroundColor = Color.White;
        dgvBookings.BorderStyle = BorderStyle.None;
        dgvBookings.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvBookings.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvBookings.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvBookings.Columns.AddRange(new DataGridViewColumn[] {
            bookingCodeDataGridViewTextBoxColumn,
            clientNameDataGridViewTextBoxColumn,
            garmentSummaryDataGridViewTextBoxColumn,
            rentalPeriodDataGridViewTextBoxColumn,
            feeDepositDataGridViewTextBoxColumn,
            stageDataGridViewTextBoxColumn,
            actionDataGridViewTextBoxColumn
        });
        dgvBookings.Dock = DockStyle.Fill;
        dgvBookings.GridColor = Color.FromArgb(242, 235, 235);
        dgvBookings.Location = new Point(1, 1);
        dgvBookings.MultiSelect = false;
        dgvBookings.Name = "dgvBookings";
        dgvBookings.ReadOnly = true;
        dgvBookings.RowHeadersVisible = false;
        dgvBookings.RowTemplate.Height = 44;
        dgvBookings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvBookings.Size = new Size(1134, 458);
        dgvBookings.TabIndex = 0;

        // 
        // Column Configurations
        // 
        bookingCodeDataGridViewTextBoxColumn.DataPropertyName = "BookingCode";
        bookingCodeDataGridViewTextBoxColumn.FillWeight = 85F;
        bookingCodeDataGridViewTextBoxColumn.HeaderText = "CODE";
        bookingCodeDataGridViewTextBoxColumn.Name = "bookingCodeDataGridViewTextBoxColumn";
        bookingCodeDataGridViewTextBoxColumn.ReadOnly = true;

        clientNameDataGridViewTextBoxColumn.DataPropertyName = "ClientName";
        clientNameDataGridViewTextBoxColumn.FillWeight = 130F;
        clientNameDataGridViewTextBoxColumn.HeaderText = "CLIENT";
        clientNameDataGridViewTextBoxColumn.Name = "clientNameDataGridViewTextBoxColumn";
        clientNameDataGridViewTextBoxColumn.ReadOnly = true;

        garmentSummaryDataGridViewTextBoxColumn.DataPropertyName = "GarmentSummary";
        garmentSummaryDataGridViewTextBoxColumn.FillWeight = 180F;
        garmentSummaryDataGridViewTextBoxColumn.HeaderText = "GARMENT";
        garmentSummaryDataGridViewTextBoxColumn.Name = "garmentSummaryDataGridViewTextBoxColumn";
        garmentSummaryDataGridViewTextBoxColumn.ReadOnly = true;

        rentalPeriodDataGridViewTextBoxColumn.DataPropertyName = "RentalPeriod";
        rentalPeriodDataGridViewTextBoxColumn.FillWeight = 150F;
        rentalPeriodDataGridViewTextBoxColumn.HeaderText = "RENTAL PERIOD";
        rentalPeriodDataGridViewTextBoxColumn.Name = "rentalPeriodDataGridViewTextBoxColumn";
        rentalPeriodDataGridViewTextBoxColumn.ReadOnly = true;

        feeDepositDataGridViewTextBoxColumn.DataPropertyName = "FeeDeposit";
        feeDepositDataGridViewTextBoxColumn.FillWeight = 110F;
        feeDepositDataGridViewTextBoxColumn.HeaderText = "FEE / DEPOSIT";
        feeDepositDataGridViewTextBoxColumn.Name = "feeDepositDataGridViewTextBoxColumn";
        feeDepositDataGridViewTextBoxColumn.ReadOnly = true;

        stageDataGridViewTextBoxColumn.DataPropertyName = "Stage";
        stageDataGridViewTextBoxColumn.FillWeight = 90F;
        stageDataGridViewTextBoxColumn.HeaderText = "STAGE";
        stageDataGridViewTextBoxColumn.Name = "stageDataGridViewTextBoxColumn";
        stageDataGridViewTextBoxColumn.ReadOnly = true;

        actionDataGridViewTextBoxColumn.DataPropertyName = "ActionText";
        actionDataGridViewTextBoxColumn.FillWeight = 110F;
        actionDataGridViewTextBoxColumn.HeaderText = "ACTIONS";
        actionDataGridViewTextBoxColumn.Name = "actionDataGridViewTextBoxColumn";
        actionDataGridViewTextBoxColumn.ReadOnly = true;

        // 
        // RentalBookingsView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(pnlGridCard);
        Controls.Add(pnlFilterStrip);
        Controls.Add(tblKpis);
        Controls.Add(lblSubtitle);
        Controls.Add(primaryButtonNewBooking);
        Controls.Add(label1);
        Name = "RentalBookingsView";
        Padding = new Padding(32, 24, 32, 24);
        Size = new Size(1200, 760);
        Load += RentalBookingsView_Load;
        tblKpis.ResumeLayout(false);
        pnlFilterStrip.ResumeLayout(false);
        pnlSearch.ResumeLayout(false);
        pnlSearch.PerformLayout();
        pnlGridCard.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvBookings).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Label lblSubtitle;
    private Controls.PrimaryButton primaryButtonNewBooking;
    private TableLayoutPanel tblKpis;
    private Controls.KpiCardControl kpiCardControlActiveLeases;
    private Controls.KpiCardControl kpiCardControlOverdue;
    private Controls.KpiCardControl kpiCardControlUpcomingReturns;
    private Panel pnlFilterStrip;
    private FlowLayoutPanel pnlFilterTabs;
    private Panel pnlSearch;
    private TextBox txtSearch;
    private Panel pnlGridCard;
    private DataGridView dgvBookings;
    private DataGridViewTextBoxColumn bookingCodeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn clientNameDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn garmentSummaryDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn rentalPeriodDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn feeDepositDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn stageDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn actionDataGridViewTextBoxColumn;
}