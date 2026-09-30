namespace CRM.winforms.Views;

partial class Step2GarmentDatesView
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
        lblTitle = new Label();
        lblStartDate = new Label();
        dtpStartDate = new DateTimePicker();
        lblEndDate = new Label();
        dtpEndDate = new DateTimePicker();
        lblAvailableGarments = new Label();
        listBoxGarments = new ListBox();
        SuspendLayout();
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.FromArgb(38, 22, 24);
        lblTitle.Location = new Point(32, 20);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(256, 30);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Select Garment & Dates";
        lblTitle.UseMnemonic = false;
        // 
        // lblStartDate
        // 
        lblStartDate.AutoSize = true;
        lblStartDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblStartDate.ForeColor = Color.FromArgb(145, 135, 140);
        lblStartDate.Location = new Point(32, 64);
        lblStartDate.Name = "lblStartDate";
        lblStartDate.Size = new Size(97, 15);
        lblStartDate.TabIndex = 1;
        lblStartDate.Text = "Rental Start Date";
        lblStartDate.UseMnemonic = false;
        // 
        // dtpStartDate
        // 
        dtpStartDate.CalendarFont = new Font("Segoe UI", 9.5F);
        dtpStartDate.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dtpStartDate.Format = DateTimePickerFormat.Short;
        dtpStartDate.Location = new Point(32, 86);
        dtpStartDate.Name = "dtpStartDate";
        dtpStartDate.Size = new Size(240, 29);
        dtpStartDate.TabIndex = 2;
        // 
        // lblEndDate
        // 
        lblEndDate.AutoSize = true;
        lblEndDate.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lblEndDate.ForeColor = Color.FromArgb(145, 135, 140);
        lblEndDate.Location = new Point(296, 64);
        lblEndDate.Name = "lblEndDate";
        lblEndDate.Size = new Size(93, 15);
        lblEndDate.TabIndex = 3;
        lblEndDate.Text = "Rental End Date";
        lblEndDate.UseMnemonic = false;
        // 
        // dtpEndDate
        // 
        dtpEndDate.CalendarFont = new Font("Segoe UI", 9.5F);
        dtpEndDate.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dtpEndDate.Format = DateTimePickerFormat.Short;
        dtpEndDate.Location = new Point(296, 86);
        dtpEndDate.Name = "dtpEndDate";
        dtpEndDate.Size = new Size(240, 29);
        dtpEndDate.TabIndex = 4;
        // 
        // lblAvailableGarments
        // 
        lblAvailableGarments.AutoSize = true;
        lblAvailableGarments.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblAvailableGarments.ForeColor = Color.FromArgb(140, 124, 126);
        lblAvailableGarments.Location = new Point(32, 134);
        lblAvailableGarments.Name = "lblAvailableGarments";
        lblAvailableGarments.Size = new Size(130, 15);
        lblAvailableGarments.TabIndex = 5;
        lblAvailableGarments.Text = "AVAILABLE GARMENTS";
        lblAvailableGarments.UseMnemonic = false;
        // 
        // listBoxGarments
        // 
        listBoxGarments.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        listBoxGarments.BackColor = Color.FromArgb(249, 241, 241);
        listBoxGarments.BorderStyle = BorderStyle.None;
        listBoxGarments.DrawMode = DrawMode.OwnerDrawFixed;
        listBoxGarments.FormattingEnabled = true;
        listBoxGarments.IntegralHeight = false;
        listBoxGarments.ItemHeight = 68;
        listBoxGarments.Location = new Point(32, 158);
        listBoxGarments.Name = "listBoxGarments";
        listBoxGarments.Size = new Size(1546, 455);
        listBoxGarments.TabIndex = 6;
        // 
        // Step2GarmentDatesView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(listBoxGarments);
        Controls.Add(lblAvailableGarments);
        Controls.Add(dtpEndDate);
        Controls.Add(lblEndDate);
        Controls.Add(dtpStartDate);
        Controls.Add(lblStartDate);
        Controls.Add(lblTitle);
        Name = "Step2GarmentDatesView";
        Padding = new Padding(32, 20, 32, 20);
        Size = new Size(1610, 645);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTitle;
    private Label lblStartDate;
    private DateTimePicker dtpStartDate;
    private Label lblEndDate;
    private DateTimePicker dtpEndDate;
    private Label lblAvailableGarments;
    private ListBox listBoxGarments;
}