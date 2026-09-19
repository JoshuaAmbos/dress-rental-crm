namespace CRM.winforms.Views
{
    partial class RentalBookingsView
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
            kpiCardControl1 = new CRM.winforms.Controls.KpiCardControl();
            kpiCardControl2 = new CRM.winforms.Controls.KpiCardControl();
            kpiCardControl3 = new CRM.winforms.Controls.KpiCardControl();
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
            // kpiCardControl1
            // 
            kpiCardControl1.BackColor = Color.Transparent;
            kpiCardControl1.Location = new Point(33, 93);
            kpiCardControl1.Name = "kpiCardControl1";
            kpiCardControl1.Padding = new Padding(14);
            kpiCardControl1.Size = new Size(518, 183);
            kpiCardControl1.TabIndex = 2;
            // 
            // kpiCardControl2
            // 
            kpiCardControl2.BackColor = Color.Transparent;
            kpiCardControl2.Location = new Point(596, 93);
            kpiCardControl2.Name = "kpiCardControl2";
            kpiCardControl2.Padding = new Padding(14);
            kpiCardControl2.Size = new Size(518, 183);
            kpiCardControl2.TabIndex = 3;
            // 
            // kpiCardControl3
            // 
            kpiCardControl3.BackColor = Color.Transparent;
            kpiCardControl3.Location = new Point(1161, 93);
            kpiCardControl3.Name = "kpiCardControl3";
            kpiCardControl3.Padding = new Padding(14);
            kpiCardControl3.Size = new Size(518, 183);
            kpiCardControl3.TabIndex = 4;
            // 
            // RentalBookingsView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(249, 241, 241);
            Controls.Add(kpiCardControl3);
            Controls.Add(kpiCardControl2);
            Controls.Add(kpiCardControl1);
            Controls.Add(label1);
            Name = "RentalBookingsView";
            Padding = new Padding(30);
            Size = new Size(1712, 955);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Controls.KpiCardControl kpiCardControl1;
        private Controls.KpiCardControl kpiCardControl2;
        private Controls.KpiCardControl kpiCardControl3;
    }
}
