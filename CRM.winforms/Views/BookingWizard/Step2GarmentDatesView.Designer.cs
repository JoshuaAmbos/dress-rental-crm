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
        lblTitle.Font = new Font("Segoe UI Semibold", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
        lblTitle.ForeColor = Color.FromArgb(38, 22, 24);
        lblTitle.Location = new Point(0, 0);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(261, 30);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Select Garment and Dates";
        // 
        // lblStartDate
        // 
        lblStartDate.AutoSize = true;
        lblStartDate.Font = new Font("Segoe UI", 9F);
        lblStartDate.ForeColor = Color.FromArgb(145, 135, 140);
        lblStartDate.Location = new Point(2, 68);
        lblStartDate.Name = "lblStartDate";
        lblStartDate.Size = new Size(94, 15);
        lblStartDate.TabIndex = 2;
        lblStartDate.Text = "Rental Start Date";
        // 
        // dtpStartDate
        // 
        dtpStartDate.CalendarFont = new Font("Segoe UI", 9.5F);
        dtpStartDate.Font = new Font("Segoe UI", 9.5F);
        dtpStartDate.Format = DateTimePickerFormat.Short;
        dtpStartDate.Location = new Point(2, 88);
        dtpStartDate.Name = "dtpStartDate";
        dtpStartDate.Size = new Size(200, 24);
        dtpStartDate.TabIndex = 3;
        // 
        // lblEndDate
        // 
        lblEndDate.AutoSize = true;
        lblEndDate.Font = new Font("Segoe UI", 9F);
        lblEndDate.ForeColor = Color.FromArgb(145, 135, 140);
        lblEndDate.Location = new Point(220, 68);
        lblEndDate.Name = "lblEndDate";
        lblEndDate.Size = new Size(90, 15);
        lblEndDate.TabIndex = 4;
        lblEndDate.Text = "Rental End Date";
        // 
        // dtpEndDate
        // 
        dtpEndDate.CalendarFont = new Font("Segoe UI", 9.5F);
        dtpEndDate.Font = new Font("Segoe UI", 9.5F);
        dtpEndDate.Format = DateTimePickerFormat.Short;
        dtpEndDate.Location = new Point(220, 88);
        dtpEndDate.Name = "dtpEndDate";
        dtpEndDate.Size = new Size(200, 24);
        dtpEndDate.TabIndex = 5;
        // 
        // lblAvailableGarments
        // 
        lblAvailableGarments.AutoSize = true;
        lblAvailableGarments.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
        lblAvailableGarments.ForeColor = Color.FromArgb(145, 135, 140);
        lblAvailableGarments.Location = new Point(2, 130);
        lblAvailableGarments.Name = "lblAvailableGarments";
        lblAvailableGarments.Size = new Size(132, 15);
        lblAvailableGarments.TabIndex = 6;
        lblAvailableGarments.Text = "AVAILABLE GARMENTS";
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
        listBoxGarments.Location = new Point(2, 154);
        listBoxGarments.Name = "listBoxGarments";
        listBoxGarments.Size = new Size(1600, 480);
        listBoxGarments.TabIndex = 7;
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