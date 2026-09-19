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
            buttonRentals = new Button();
            buttonCatalog = new Button();
            buttonInquiries = new Button();
            buttonLoyaltyAwards = new Button();
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
            logo.Click += Logo_Click;
            // 
            // flowLayoutPanelSidebar
            // 
            flowLayoutPanelSidebar.BackColor = Color.White;
            flowLayoutPanelSidebar.Controls.Add(buttonDashboard);
            flowLayoutPanelSidebar.Controls.Add(buttonCustomers);
            flowLayoutPanelSidebar.Controls.Add(buttonRentals);
            flowLayoutPanelSidebar.Controls.Add(buttonCatalog);
            flowLayoutPanelSidebar.Controls.Add(buttonInquiries);
            flowLayoutPanelSidebar.Controls.Add(buttonLoyaltyAwards);
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
            buttonDashboard.Click += ButtonDashboard_Click;
            // 
            // buttonCustomers
            // 
            buttonCustomers.Location = new Point(11, 68);
            buttonCustomers.Name = "buttonCustomers";
            buttonCustomers.Size = new Size(180, 39);
            buttonCustomers.TabIndex = 3;
            buttonCustomers.Text = "Customers";
            buttonCustomers.UseVisualStyleBackColor = true;
            buttonCustomers.Click += ButtonCustomers_Click;
            // 
            // buttonRentals
            // 
            buttonRentals.Location = new Point(11, 113);
            buttonRentals.Name = "buttonRentals";
            buttonRentals.Size = new Size(180, 39);
            buttonRentals.TabIndex = 4;
            buttonRentals.Text = "Rentals";
            buttonRentals.UseVisualStyleBackColor = true;
            buttonRentals.Click += ButtonRentals_Click;
            // 
            // buttonCatalog
            // 
            buttonCatalog.Location = new Point(11, 158);
            buttonCatalog.Name = "buttonCatalog";
            buttonCatalog.Size = new Size(180, 39);
            buttonCatalog.TabIndex = 5;
            buttonCatalog.Text = "Catalog";
            buttonCatalog.UseVisualStyleBackColor = true;
            buttonCatalog.Click += ButtonCatalog_Click;
            // 
            // buttonInquiries
            // 
            buttonInquiries.Location = new Point(11, 203);
            buttonInquiries.Name = "buttonInquiries";
            buttonInquiries.Size = new Size(180, 39);
            buttonInquiries.TabIndex = 6;
            buttonInquiries.Text = "Inquiries";
            buttonInquiries.UseVisualStyleBackColor = true;
            // 
            // buttonLoyaltyAwards
            // 
            buttonLoyaltyAwards.Location = new Point(11, 248);
            buttonLoyaltyAwards.Name = "buttonLoyaltyAwards";
            buttonLoyaltyAwards.Size = new Size(180, 39);
            buttonLoyaltyAwards.TabIndex = 7;
            buttonLoyaltyAwards.Text = "Loyalty Awards";
            buttonLoyaltyAwards.UseVisualStyleBackColor = true;
            // 
            // panelContents
            // 
            panelContents.BackColor = Color.Transparent;
            panelContents.Location = new Point(194, 87);
            panelContents.Name = "panelContents";
            panelContents.Size = new Size(1712, 955);
            panelContents.TabIndex = 3;
            panelContents.Paint += PanelContents_Paint;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
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
        private Button buttonRentals;
        private Panel panelContents;
        private Button buttonCatalog;
        private Button buttonInquiries;
        private Button buttonLoyaltyAwards;
    }
}