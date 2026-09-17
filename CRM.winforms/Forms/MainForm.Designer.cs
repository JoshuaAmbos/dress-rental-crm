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
            panelHeader = new Panel();
            logo = new PictureBox();
            flowLayoutPanelSidebar = new FlowLayoutPanel();
            buttonDashboard = new Button();
            buttonCustomers = new Button();
            button2 = new Button();
            panelContents = new Panel();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logo).BeginInit();
            flowLayoutPanelSidebar.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(logo);
            panelHeader.Location = new Point(-3, -20);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(1925, 110);
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
            // flowLayoutPanelSidebar
            // 
            flowLayoutPanelSidebar.BackColor = Color.White;
            flowLayoutPanelSidebar.Controls.Add(buttonDashboard);
            flowLayoutPanelSidebar.Controls.Add(buttonCustomers);
            flowLayoutPanelSidebar.Controls.Add(button2);
            flowLayoutPanelSidebar.Location = new Point(-3, 87);
            flowLayoutPanelSidebar.Name = "flowLayoutPanelSidebar";
            flowLayoutPanelSidebar.Padding = new Padding(8, 20, 0, 0);
            flowLayoutPanelSidebar.Size = new Size(201, 955);
            flowLayoutPanelSidebar.TabIndex = 2;
            // 
            // buttonDashboard
            // 
            buttonDashboard.Location = new Point(11, 23);
            buttonDashboard.Name = "buttonDashboard";
            buttonDashboard.Size = new Size(180, 39);
            buttonDashboard.TabIndex = 2;
            buttonDashboard.Text = "Dashboard";
            buttonDashboard.UseVisualStyleBackColor = true;
            buttonDashboard.Click += buttonDashboard_Click;
            // 
            // buttonCustomers
            // 
            buttonCustomers.Location = new Point(11, 68);
            buttonCustomers.Name = "buttonCustomers";
            buttonCustomers.Size = new Size(180, 39);
            buttonCustomers.TabIndex = 3;
            buttonCustomers.Text = "Customers";
            buttonCustomers.UseVisualStyleBackColor = true;
            buttonCustomers.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(11, 113);
            button2.Name = "button2";
            button2.Size = new Size(180, 39);
            button2.TabIndex = 4;
            button2.Text = "Dashboard";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // panelContents
            // 
            panelContents.BackColor = Color.Transparent;
            panelContents.Location = new Point(194, 87);
            panelContents.Name = "panelContents";
            panelContents.Size = new Size(1712, 955);
            panelContents.TabIndex = 3;
            panelContents.Paint += panelContents_Paint;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            ClientSize = new Size(1904, 1041);
            Controls.Add(panelContents);
            Controls.Add(flowLayoutPanelSidebar);
            Controls.Add(panelHeader);
            MinimumSize = new Size(1100, 700);
            Name = "MainForm";
            Text = "Vantage CRM";
            WindowState = FormWindowState.Maximized;
            Load += MainForm_Load;
            panelHeader.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)logo).EndInit();
            flowLayoutPanelSidebar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Panel panelHeader;
        private PictureBox logo;
        private FlowLayoutPanel flowLayoutPanelSidebar;
        private Button buttonDashboard;
        private Button buttonCustomers;
        private Button button2;
        private Panel panelContents;
    }
}