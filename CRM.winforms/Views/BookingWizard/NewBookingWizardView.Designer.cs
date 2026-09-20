namespace CRM.winforms.Views;

partial class NewBookingWizardView
{
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

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
        label1 = new Label();
        wizardStepperControl = new CRM.winforms.Controls.WizardStepperControl();
        label3 = new Label();
        btnCancel = new CRM.winforms.Controls.SecondaryButton();
        btnNext = new CRM.winforms.Controls.PrimaryButton();
        panelContentHost = new Panel();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(33, 28);
        label1.Name = "label1";
        label1.Size = new Size(189, 37);
        label1.TabIndex = 3;
        label1.Text = "New Booking";
        // 
        // wizardStepperControl
        // 
        wizardStepperControl.BackColor = Color.FromArgb(249, 241, 241);
        wizardStepperControl.Location = new Point(0, 111);
        wizardStepperControl.Name = "wizardStepperControl";
        wizardStepperControl.Size = new Size(1712, 97);
        wizardStepperControl.TabIndex = 4;
        wizardStepperControl.Text = "wizardStepperControl1";
        // 
        // label3
        // 
        label3.AutoSize = true;
        label3.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
        label3.ForeColor = Color.Gray;
        label3.Location = new Point(38, 65);
        label3.Name = "label3";
        label3.Size = new Size(383, 21);
        label3.TabIndex = 6;
        label3.Text = "Complete the intake wizard to create a rental booking.";
        // 
        // btnCancel
        // 
        btnCancel.BackColor = Color.Transparent;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnCancel.ForeColor = Color.FromArgb(90, 75, 80);
        btnCancel.Location = new Point(67, 839);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(110, 38);
        btnCancel.TabIndex = 9;
        btnCancel.Text = "← Cancel";
        btnCancel.UseVisualStyleBackColor = false;
        // 
        // btnNext
        // 
        btnNext.BackColor = Color.Transparent;
        btnNext.FlatAppearance.BorderSize = 0;
        btnNext.FlatStyle = FlatStyle.Flat;
        btnNext.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnNext.ForeColor = Color.White;
        btnNext.Location = new Point(1519, 839);
        btnNext.Name = "btnNext";
        btnNext.Size = new Size(130, 38);
        btnNext.TabIndex = 10;
        btnNext.Text = "Continue →";
        btnNext.UseVisualStyleBackColor = false;
        // 
        // panelContentHost
        // 
        panelContentHost.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelContentHost.BackColor = Color.Transparent;
        panelContentHost.Location = new Point(38, 254);
        panelContentHost.Name = "panelContentHost";
        panelContentHost.Size = new Size(1641, 546);
        panelContentHost.TabIndex = 11;
        // 
        // NewBookingWizardView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(btnNext);
        Controls.Add(btnCancel);
        Controls.Add(label3);
        Controls.Add(wizardStepperControl);
        Controls.Add(label1);
        Controls.Add(panelContentHost);
        Name = "NewBookingWizardView";
        Padding = new Padding(30);
        Size = new Size(1712, 955);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Controls.WizardStepperControl wizardStepperControl;
    private Label label3;
    private Controls.SecondaryButton btnCancel;
    private Controls.PrimaryButton btnNext;
    private Panel panelContentHost;
}
