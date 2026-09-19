namespace CRM.winforms.Views
{
    partial class CatalogView
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
            searchBar1 = new CRM.winforms.Controls.SearchBar();
            secondaryButtonAll = new CRM.winforms.Controls.SecondaryButton();
            secondaryButtonAvailable = new CRM.winforms.Controls.SecondaryButton();
            secondaryButtonRented = new CRM.winforms.Controls.SecondaryButton();
            secondaryButtonCleaning = new CRM.winforms.Controls.SecondaryButton();
            secondaryButtonAlterations = new CRM.winforms.Controls.SecondaryButton();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.FromArgb(38, 22, 24);
            label1.Location = new Point(33, 28);
            label1.Name = "label1";
            label1.Size = new Size(236, 37);
            label1.TabIndex = 2;
            label1.Text = "Garment Catalog";
            // 
            // searchBar1
            // 
            searchBar1.BackColor = Color.Transparent;
            searchBar1.Location = new Point(33, 91);
            searchBar1.Name = "searchBar1";
            searchBar1.Size = new Size(320, 34);
            searchBar1.TabIndex = 3;
            // 
            // secondaryButtonAll
            // 
            secondaryButtonAll.BackColor = Color.Transparent;
            secondaryButtonAll.FlatAppearance.BorderSize = 0;
            secondaryButtonAll.FlatStyle = FlatStyle.Flat;
            secondaryButtonAll.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            secondaryButtonAll.ForeColor = Color.FromArgb(90, 75, 80);
            secondaryButtonAll.Location = new Point(359, 91);
            secondaryButtonAll.Name = "secondaryButtonAll";
            secondaryButtonAll.Size = new Size(109, 34);
            secondaryButtonAll.TabIndex = 4;
            secondaryButtonAll.Text = "All";
            secondaryButtonAll.UseVisualStyleBackColor = false;
            secondaryButtonAll.Click += secondaryButtonAll_Click;
            // 
            // secondaryButtonAvailable
            // 
            secondaryButtonAvailable.BackColor = Color.Transparent;
            secondaryButtonAvailable.FlatAppearance.BorderSize = 0;
            secondaryButtonAvailable.FlatStyle = FlatStyle.Flat;
            secondaryButtonAvailable.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            secondaryButtonAvailable.ForeColor = Color.FromArgb(90, 75, 80);
            secondaryButtonAvailable.Location = new Point(474, 91);
            secondaryButtonAvailable.Name = "secondaryButtonAvailable";
            secondaryButtonAvailable.Size = new Size(109, 34);
            secondaryButtonAvailable.TabIndex = 5;
            secondaryButtonAvailable.Text = "Available";
            secondaryButtonAvailable.UseVisualStyleBackColor = false;
            // 
            // secondaryButtonRented
            // 
            secondaryButtonRented.BackColor = Color.Transparent;
            secondaryButtonRented.FlatAppearance.BorderSize = 0;
            secondaryButtonRented.FlatStyle = FlatStyle.Flat;
            secondaryButtonRented.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            secondaryButtonRented.ForeColor = Color.FromArgb(90, 75, 80);
            secondaryButtonRented.Location = new Point(589, 91);
            secondaryButtonRented.Name = "secondaryButtonRented";
            secondaryButtonRented.Size = new Size(109, 34);
            secondaryButtonRented.TabIndex = 6;
            secondaryButtonRented.Text = "RentedOut";
            secondaryButtonRented.UseVisualStyleBackColor = false;
            // 
            // secondaryButtonCleaning
            // 
            secondaryButtonCleaning.BackColor = Color.Transparent;
            secondaryButtonCleaning.FlatAppearance.BorderSize = 0;
            secondaryButtonCleaning.FlatStyle = FlatStyle.Flat;
            secondaryButtonCleaning.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            secondaryButtonCleaning.ForeColor = Color.FromArgb(90, 75, 80);
            secondaryButtonCleaning.Location = new Point(704, 91);
            secondaryButtonCleaning.Name = "secondaryButtonCleaning";
            secondaryButtonCleaning.Size = new Size(109, 34);
            secondaryButtonCleaning.TabIndex = 7;
            secondaryButtonCleaning.Text = "In Cleaning";
            secondaryButtonCleaning.UseVisualStyleBackColor = false;
            // 
            // secondaryButtonAlterations
            // 
            secondaryButtonAlterations.BackColor = Color.Transparent;
            secondaryButtonAlterations.FlatAppearance.BorderSize = 0;
            secondaryButtonAlterations.FlatStyle = FlatStyle.Flat;
            secondaryButtonAlterations.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            secondaryButtonAlterations.ForeColor = Color.FromArgb(90, 75, 80);
            secondaryButtonAlterations.Location = new Point(819, 91);
            secondaryButtonAlterations.Name = "secondaryButtonAlterations";
            secondaryButtonAlterations.Size = new Size(109, 34);
            secondaryButtonAlterations.TabIndex = 8;
            secondaryButtonAlterations.Text = "Alterations";
            secondaryButtonAlterations.UseVisualStyleBackColor = false;
            // 
            // CatalogView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(249, 241, 241);
            Controls.Add(secondaryButtonAlterations);
            Controls.Add(secondaryButtonCleaning);
            Controls.Add(secondaryButtonRented);
            Controls.Add(secondaryButtonAvailable);
            Controls.Add(secondaryButtonAll);
            Controls.Add(searchBar1);
            Controls.Add(label1);
            Name = "CatalogView";
            Padding = new Padding(30);
            Size = new Size(1712, 955);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Controls.SearchBar searchBar1;
        private Controls.SecondaryButton secondaryButtonAll;
        private Controls.SecondaryButton secondaryButtonAvailable;
        private Controls.SecondaryButton secondaryButtonRented;
        private Controls.SecondaryButton secondaryButtonCleaning;
        private Controls.SecondaryButton secondaryButtonAlterations;
    }
}
