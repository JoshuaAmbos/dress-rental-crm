namespace CRM.winforms.Views;

partial class RentalBookingsView
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null!;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        label1 = new Label();
        kpiCardControlActiveLeases = new CRM.winforms.Controls.KpiCardControl();
        kpiCardControlOverdue = new CRM.winforms.Controls.KpiCardControl();
        kpiCardControlUpcomingReturns = new CRM.winforms.Controls.KpiCardControl();
        primaryButtonNewBooking = new CRM.winforms.Controls.PrimaryButton();
        dgvBookings = new DataGridView();
        bookingCodeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        clientNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        garmentSummaryDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        rentalPeriodDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        feeDepositDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        stageDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        actionDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        ((System.ComponentModel.ISupportInitialize)dgvBookings).BeginInit();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(33, 28);
        label1.Name = "label1";
        label1.Size = new Size(382, 37);
        label1.TabIndex = 1;
        label1.Text = "Rentals and Returns Pipeline";
        label1.Click += label1_Click;
        // 
        // kpiCardControlActiveLeases
        // 
        kpiCardControlActiveLeases.BackColor = Color.Transparent;
        kpiCardControlActiveLeases.Location = new Point(33, 93);
        kpiCardControlActiveLeases.Name = "kpiCardControlActiveLeases";
        kpiCardControlActiveLeases.Padding = new Padding(14);
        kpiCardControlActiveLeases.Size = new Size(518, 183);
        kpiCardControlActiveLeases.TabIndex = 2;
        // 
        // kpiCardControlOverdue
        // 
        kpiCardControlOverdue.BackColor = Color.Transparent;
        kpiCardControlOverdue.Location = new Point(596, 93);
        kpiCardControlOverdue.Name = "kpiCardControlOverdue";
        kpiCardControlOverdue.Padding = new Padding(14);
        kpiCardControlOverdue.Size = new Size(518, 183);
        kpiCardControlOverdue.TabIndex = 3;
        // 
        // kpiCardControlUpcomingReturns
        // 
        kpiCardControlUpcomingReturns.BackColor = Color.Transparent;
        kpiCardControlUpcomingReturns.Location = new Point(1161, 93);
        kpiCardControlUpcomingReturns.Name = "kpiCardControlUpcomingReturns";
        kpiCardControlUpcomingReturns.Padding = new Padding(14);
        kpiCardControlUpcomingReturns.Size = new Size(518, 183);
        kpiCardControlUpcomingReturns.TabIndex = 4;
        // 
        // primaryButtonNewBooking
        // 
        primaryButtonNewBooking.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        primaryButtonNewBooking.BackColor = Color.FromArgb(190, 110, 120);
        primaryButtonNewBooking.FlatAppearance.BorderSize = 0;
        primaryButtonNewBooking.FlatStyle = FlatStyle.Flat;
        primaryButtonNewBooking.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        primaryButtonNewBooking.ForeColor = Color.White;
        primaryButtonNewBooking.Location = new Point(1549, 27);
        primaryButtonNewBooking.Name = "primaryButtonNewBooking";
        primaryButtonNewBooking.Size = new Size(130, 38);
        primaryButtonNewBooking.TabIndex = 5;
        primaryButtonNewBooking.Text = "+ New Booking";
        primaryButtonNewBooking.UseVisualStyleBackColor = false;
        primaryButtonNewBooking.Click += PrimaryButtonNewBooking_Click;
        // 
        // dgvBookings
        // 
        dgvBookings.AllowUserToAddRows = false;
        dgvBookings.AllowUserToDeleteRows = false;
        dgvBookings.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 244, 244);
        dataGridViewCellStyle1.ForeColor = Color.FromArgb(44, 34, 38);
        dataGridViewCellStyle1.Padding = new Padding(12, 2, 12, 2);
        dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(190, 110, 120);
        dataGridViewCellStyle1.SelectionForeColor = Color.White;
        dgvBookings.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dgvBookings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvBookings.AutoGenerateColumns = false;
        dgvBookings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvBookings.BackgroundColor = Color.White;
        dgvBookings.BorderStyle = BorderStyle.Fixed3D;
        dgvBookings.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvBookings.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(249, 241, 241);
        dataGridViewCellStyle2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(38, 22, 24);
        dataGridViewCellStyle2.Padding = new Padding(12, 0, 12, 0);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(249, 241, 241);
        dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(38, 22, 24);
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        dgvBookings.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvBookings.ColumnHeadersHeight = 44;
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
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(44, 34, 38);
        dataGridViewCellStyle3.Padding = new Padding(12, 2, 12, 2);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(190, 110, 120);
        dataGridViewCellStyle3.SelectionForeColor = Color.White;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        dgvBookings.DefaultCellStyle = dataGridViewCellStyle3;
        dgvBookings.EnableHeadersVisualStyles = false;
        dgvBookings.GridColor = Color.FromArgb(234, 223, 217);
        dgvBookings.Location = new Point(33, 298);
        dgvBookings.MultiSelect = false;
        dgvBookings.Name = "dgvBookings";
        dgvBookings.ReadOnly = true;
        dgvBookings.RowHeadersVisible = false;
        dgvBookings.RowTemplate.Height = 44;
        dgvBookings.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvBookings.Size = new Size(1646, 624);
        dgvBookings.TabIndex = 7;
        // 
        // bookingCodeDataGridViewTextBoxColumn
        // 
        bookingCodeDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        bookingCodeDataGridViewTextBoxColumn.DataPropertyName = "BookingCode";
        bookingCodeDataGridViewTextBoxColumn.FillWeight = 85F;
        bookingCodeDataGridViewTextBoxColumn.HeaderText = "CODE";
        bookingCodeDataGridViewTextBoxColumn.Name = "bookingCodeDataGridViewTextBoxColumn";
        bookingCodeDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // clientNameDataGridViewTextBoxColumn
        // 
        clientNameDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        clientNameDataGridViewTextBoxColumn.DataPropertyName = "ClientName";
        clientNameDataGridViewTextBoxColumn.FillWeight = 130F;
        clientNameDataGridViewTextBoxColumn.HeaderText = "CLIENT";
        clientNameDataGridViewTextBoxColumn.Name = "clientNameDataGridViewTextBoxColumn";
        clientNameDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // garmentSummaryDataGridViewTextBoxColumn
        // 
        garmentSummaryDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        garmentSummaryDataGridViewTextBoxColumn.DataPropertyName = "GarmentSummary";
        garmentSummaryDataGridViewTextBoxColumn.FillWeight = 190F;
        garmentSummaryDataGridViewTextBoxColumn.HeaderText = "GARMENT";
        garmentSummaryDataGridViewTextBoxColumn.Name = "garmentSummaryDataGridViewTextBoxColumn";
        garmentSummaryDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // rentalPeriodDataGridViewTextBoxColumn
        // 
        rentalPeriodDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        rentalPeriodDataGridViewTextBoxColumn.FillWeight = 160F;
        rentalPeriodDataGridViewTextBoxColumn.HeaderText = "RENTAL PERIOD";
        rentalPeriodDataGridViewTextBoxColumn.Name = "ColRentalPeriod";
        rentalPeriodDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // feeDepositDataGridViewTextBoxColumn
        // 
        feeDepositDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        feeDepositDataGridViewTextBoxColumn.FillWeight = 110F;
        feeDepositDataGridViewTextBoxColumn.HeaderText = "FEE / DEPOSIT";
        feeDepositDataGridViewTextBoxColumn.Name = "ColFeeDeposit";
        feeDepositDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // stageDataGridViewTextBoxColumn
        // 
        stageDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        stageDataGridViewTextBoxColumn.FillWeight = 90F;
        stageDataGridViewTextBoxColumn.HeaderText = "STAGE";
        stageDataGridViewTextBoxColumn.Name = "ColStage";
        stageDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // actionDataGridViewTextBoxColumn
        // 
        actionDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        actionDataGridViewTextBoxColumn.FillWeight = 110F;
        actionDataGridViewTextBoxColumn.HeaderText = "ACTIONS";
        actionDataGridViewTextBoxColumn.Name = "ColAction";
        actionDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // RentalBookingsView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(dgvBookings);
        Controls.Add(primaryButtonNewBooking);
        Controls.Add(kpiCardControlUpcomingReturns);
        Controls.Add(kpiCardControlOverdue);
        Controls.Add(kpiCardControlActiveLeases);
        Controls.Add(label1);
        Name = "RentalBookingsView";
        Padding = new Padding(30);
        Size = new Size(1712, 955);
        Load += RentalBookingsView_Load;
        ((System.ComponentModel.ISupportInitialize)dgvBookings).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Controls.KpiCardControl kpiCardControlActiveLeases;
    private Controls.KpiCardControl kpiCardControlOverdue;
    private Controls.KpiCardControl kpiCardControlUpcomingReturns;
    private Controls.PrimaryButton primaryButtonNewBooking;
    private DataGridView dgvBookings;
    private DataGridViewTextBoxColumn bookingCodeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn clientNameDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn garmentSummaryDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn rentalPeriodDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn feeDepositDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn stageDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn actionDataGridViewTextBoxColumn;
}