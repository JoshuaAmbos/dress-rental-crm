namespace CRM.winforms.Forms
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panelSidebar = new Panel();
            button2 = new Button();
            buttonCustomers = new Button();
            buttonDashboard = new Button();
            panelHeader = new Panel();
            logo = new PictureBox();
            panelSidebar.SuspendLayout();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logo).BeginInit();
            SuspendLayout();
            // 
            // panelSidebar
            // 
            panelSidebar.BackColor = Color.White;
            panelSidebar.Controls.Add(button2);
            panelSidebar.Controls.Add(buttonCustomers);
            panelSidebar.Controls.Add(buttonDashboard);
            panelSidebar.Location = new Point(2, -2);
            panelSidebar.Name = "panelSidebar";
            panelSidebar.Size = new Size(201, 1080);
            panelSidebar.TabIndex = 0;
            panelSidebar.Paint += panel1_Paint;
            // 
            // button2
            // 
            button2.Location = new Point(10, 208);
            button2.Name = "button2";
            button2.Size = new Size(180, 39);
            button2.TabIndex = 4;
            button2.Text = "Dashboard";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // buttonCustomers
            // 
            buttonCustomers.Location = new Point(10, 163);
            buttonCustomers.Name = "buttonCustomers";
            buttonCustomers.Size = new Size(180, 39);
            buttonCustomers.TabIndex = 3;
            buttonCustomers.Text = "Customers";
            buttonCustomers.UseVisualStyleBackColor = true;
            buttonCustomers.Click += button1_Click;
            // 
            // buttonDashboard
            // 
            buttonDashboard.Location = new Point(10, 118);
            buttonDashboard.Name = "buttonDashboard";
            buttonDashboard.Size = new Size(180, 39);
            buttonDashboard.TabIndex = 2;
            buttonDashboard.Text = "Dashboard";
            buttonDashboard.UseVisualStyleBackColor = true;
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(logo);
            panelHeader.Location = new Point(2, -20);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1920, 110);
            panelHeader.TabIndex = 1;
            // 
            // logo
            // 
            logo.BackgroundImageLayout = ImageLayout.None;
            logo.Cursor = Cursors.Hand;
            logo.Image = Properties.Resources.ProjectCRM_Logo;
            logo.Location = new Point(0, 18);
            logo.Name = "logo";
            logo.Size = new Size(201, 92);
            logo.SizeMode = PictureBoxSizeMode.StretchImage;
            logo.TabIndex = 0;
            logo.TabStop = false;
            logo.Click += logo_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            ClientSize = new Size(1904, 1041);
            Controls.Add(panelHeader);
            Controls.Add(panelSidebar);
            MinimumSize = new Size(1100, 700);
            Name = "MainForm";
            Text = "Vantage CRM";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            panelSidebar.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)logo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panelSidebar;
        private Panel panelHeader;
        private PictureBox logo;
        private Button buttonCustomers;
        private Button buttonDashboard;
        private Button button2;
    }
}