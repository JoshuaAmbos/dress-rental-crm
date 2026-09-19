namespace CRM.winforms.Views
{
    partial class CustomerProfilesView
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
            components = new System.ComponentModel.Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            label1 = new Label();
            primaryButtonNewCustomer = new CRM.winforms.Controls.PrimaryButton();
            dgvCustomers = new CRM.winforms.Controls.ModernDataGridView();
            customerBindingSource1 = new BindingSource(components);
            customerBindingSource = new BindingSource(components);
            companyDatabaseBindingSource = new BindingSource(components);
            searchBar1 = new CRM.winforms.Controls.SearchBar();
            chkShowArchived = new CRM.winforms.Controls.AtelierCheckBox();
            customerBindingSource2 = new BindingSource(components);
            customerIdDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            customerCodeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            companyIdDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            firstNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            middleNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            lastNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            contactNumberDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            emailAddressDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            addressDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            bustSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            waistSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            hipSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            createdAtDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            rentalBookingsDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)companyDatabaseBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource2).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(33, 28);
            label1.Name = "label1";
            label1.Size = new Size(153, 37);
            label1.TabIndex = 0;
            label1.Text = "Customers";
            label1.Click += Label1_Click;
            // 
            // primaryButtonNewCustomer
            // 
            primaryButtonNewCustomer.BackColor = Color.Transparent;
            primaryButtonNewCustomer.FlatAppearance.BorderSize = 0;
            primaryButtonNewCustomer.FlatStyle = FlatStyle.Flat;
            primaryButtonNewCustomer.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            primaryButtonNewCustomer.ForeColor = Color.White;
            primaryButtonNewCustomer.Location = new Point(1549, 89);
            primaryButtonNewCustomer.Name = "primaryButtonNewCustomer";
            primaryButtonNewCustomer.Size = new Size(130, 38);
            primaryButtonNewCustomer.TabIndex = 3;
            primaryButtonNewCustomer.Text = "+ New Customer";
            primaryButtonNewCustomer.UseVisualStyleBackColor = false;
            // 
            // dgvCustomers
            // 
            dgvCustomers.AllowUserToAddRows = false;
            dgvCustomers.AllowUserToDeleteRows = false;
            dgvCustomers.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(253, 250, 250);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(45, 35, 40);
            dataGridViewCellStyle1.Padding = new Padding(8, 0, 0, 0);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(190, 110, 120);
            dataGridViewCellStyle1.SelectionForeColor = Color.White;
            dgvCustomers.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            dgvCustomers.AutoGenerateColumns = false;
            dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCustomers.BackgroundColor = Color.White;
            dgvCustomers.BorderStyle = BorderStyle.None;
            dgvCustomers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvCustomers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(244, 238, 238);
            dataGridViewCellStyle2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(38, 22, 24);
            dataGridViewCellStyle2.Padding = new Padding(8, 0, 0, 0);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(244, 238, 238);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(38, 22, 24);
            dgvCustomers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCustomers.Columns.AddRange(new DataGridViewColumn[] { customerIdDataGridViewTextBoxColumn, customerCodeDataGridViewTextBoxColumn, companyIdDataGridViewTextBoxColumn, firstNameDataGridViewTextBoxColumn, middleNameDataGridViewTextBoxColumn, lastNameDataGridViewTextBoxColumn, contactNumberDataGridViewTextBoxColumn, emailAddressDataGridViewTextBoxColumn, addressDataGridViewTextBoxColumn, bustSizeDataGridViewTextBoxColumn, waistSizeDataGridViewTextBoxColumn, hipSizeDataGridViewTextBoxColumn, createdAtDataGridViewTextBoxColumn, rentalBookingsDataGridViewTextBoxColumn });
            dgvCustomers.DataSource = customerBindingSource1;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.White;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9.5F);
            dataGridViewCellStyle3.ForeColor = Color.FromArgb(45, 35, 40);
            dataGridViewCellStyle3.Padding = new Padding(8, 0, 0, 0);
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(190, 110, 120);
            dataGridViewCellStyle3.SelectionForeColor = Color.White;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            dgvCustomers.DefaultCellStyle = dataGridViewCellStyle3;
            dgvCustomers.EnableHeadersVisualStyles = false;
            dgvCustomers.Font = new Font("Segoe UI", 9.5F);
            dgvCustomers.GridColor = Color.FromArgb(228, 222, 222);
            dgvCustomers.Location = new Point(33, 139);
            dgvCustomers.MultiSelect = false;
            dgvCustomers.Name = "dgvCustomers";
            dgvCustomers.ReadOnly = true;
            dgvCustomers.RowHeadersVisible = false;
            dgvCustomers.RowTemplate.Height = 32;
            dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCustomers.Size = new Size(1646, 769);
            dgvCustomers.TabIndex = 4;
            // 
            // customerBindingSource1
            // 
            customerBindingSource1.DataSource = typeof(domain.entities.Customer);
            // 
            // customerBindingSource
            // 
            customerBindingSource.DataSource = typeof(domain.entities.Customer);
            // 
            // companyDatabaseBindingSource
            // 
            companyDatabaseBindingSource.DataSource = typeof(domain.entities.CompanyDatabase);
            // 
            // searchBar1
            // 
            searchBar1.BackColor = Color.Transparent;
            searchBar1.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            searchBar1.Location = new Point(33, 93);
            searchBar1.Name = "searchBar1";
            searchBar1.Padding = new Padding(12, 8, 12, 8);
            searchBar1.Size = new Size(340, 40);
            searchBar1.TabIndex = 2;
            searchBar1.Load += SearchBar1_Load;
            // 
            // chkShowArchived
            // 
            chkShowArchived.BackColor = Color.Transparent;
            chkShowArchived.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkShowArchived.ForeColor = Color.FromArgb(45, 35, 40);
            chkShowArchived.Location = new Point(393, 102);
            chkShowArchived.Name = "chkShowArchived";
            chkShowArchived.Size = new Size(135, 25);
            chkShowArchived.TabIndex = 5;
            chkShowArchived.Text = "Show Archived";
            chkShowArchived.UseVisualStyleBackColor = false;
            chkShowArchived.CheckedChanged += ChkShowArchived_CheckedChanged_1;
            // 
            // customerBindingSource2
            // 
            customerBindingSource2.DataSource = typeof(domain.entities.Customer);
            // 
            // customerIdDataGridViewTextBoxColumn
            // 
            customerIdDataGridViewTextBoxColumn.DataPropertyName = "CustomerId";
            customerIdDataGridViewTextBoxColumn.HeaderText = "CustomerId";
            customerIdDataGridViewTextBoxColumn.Name = "customerIdDataGridViewTextBoxColumn";
            customerIdDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // customerCodeDataGridViewTextBoxColumn
            // 
            customerCodeDataGridViewTextBoxColumn.DataPropertyName = "CustomerCode";
            customerCodeDataGridViewTextBoxColumn.HeaderText = "CustomerCode";
            customerCodeDataGridViewTextBoxColumn.Name = "customerCodeDataGridViewTextBoxColumn";
            customerCodeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // companyIdDataGridViewTextBoxColumn
            // 
            companyIdDataGridViewTextBoxColumn.DataPropertyName = "CompanyId";
            companyIdDataGridViewTextBoxColumn.HeaderText = "CompanyId";
            companyIdDataGridViewTextBoxColumn.Name = "companyIdDataGridViewTextBoxColumn";
            companyIdDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // firstNameDataGridViewTextBoxColumn
            // 
            firstNameDataGridViewTextBoxColumn.DataPropertyName = "FirstName";
            firstNameDataGridViewTextBoxColumn.HeaderText = "FirstName";
            firstNameDataGridViewTextBoxColumn.Name = "firstNameDataGridViewTextBoxColumn";
            firstNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // middleNameDataGridViewTextBoxColumn
            // 
            middleNameDataGridViewTextBoxColumn.DataPropertyName = "MiddleName";
            middleNameDataGridViewTextBoxColumn.HeaderText = "MiddleName";
            middleNameDataGridViewTextBoxColumn.Name = "middleNameDataGridViewTextBoxColumn";
            middleNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // lastNameDataGridViewTextBoxColumn
            // 
            lastNameDataGridViewTextBoxColumn.DataPropertyName = "LastName";
            lastNameDataGridViewTextBoxColumn.HeaderText = "LastName";
            lastNameDataGridViewTextBoxColumn.Name = "lastNameDataGridViewTextBoxColumn";
            lastNameDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // contactNumberDataGridViewTextBoxColumn
            // 
            contactNumberDataGridViewTextBoxColumn.DataPropertyName = "ContactNumber";
            contactNumberDataGridViewTextBoxColumn.HeaderText = "ContactNumber";
            contactNumberDataGridViewTextBoxColumn.Name = "contactNumberDataGridViewTextBoxColumn";
            contactNumberDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // emailAddressDataGridViewTextBoxColumn
            // 
            emailAddressDataGridViewTextBoxColumn.DataPropertyName = "EmailAddress";
            emailAddressDataGridViewTextBoxColumn.HeaderText = "EmailAddress";
            emailAddressDataGridViewTextBoxColumn.Name = "emailAddressDataGridViewTextBoxColumn";
            emailAddressDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // addressDataGridViewTextBoxColumn
            // 
            addressDataGridViewTextBoxColumn.DataPropertyName = "Address";
            addressDataGridViewTextBoxColumn.HeaderText = "Address";
            addressDataGridViewTextBoxColumn.Name = "addressDataGridViewTextBoxColumn";
            addressDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // bustSizeDataGridViewTextBoxColumn
            // 
            bustSizeDataGridViewTextBoxColumn.DataPropertyName = "BustSize";
            bustSizeDataGridViewTextBoxColumn.HeaderText = "BustSize";
            bustSizeDataGridViewTextBoxColumn.Name = "bustSizeDataGridViewTextBoxColumn";
            bustSizeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // waistSizeDataGridViewTextBoxColumn
            // 
            waistSizeDataGridViewTextBoxColumn.DataPropertyName = "WaistSize";
            waistSizeDataGridViewTextBoxColumn.HeaderText = "WaistSize";
            waistSizeDataGridViewTextBoxColumn.Name = "waistSizeDataGridViewTextBoxColumn";
            waistSizeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // hipSizeDataGridViewTextBoxColumn
            // 
            hipSizeDataGridViewTextBoxColumn.DataPropertyName = "HipSize";
            hipSizeDataGridViewTextBoxColumn.HeaderText = "HipSize";
            hipSizeDataGridViewTextBoxColumn.Name = "hipSizeDataGridViewTextBoxColumn";
            hipSizeDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // createdAtDataGridViewTextBoxColumn
            // 
            createdAtDataGridViewTextBoxColumn.DataPropertyName = "CreatedAt";
            createdAtDataGridViewTextBoxColumn.HeaderText = "CreatedAt";
            createdAtDataGridViewTextBoxColumn.Name = "createdAtDataGridViewTextBoxColumn";
            createdAtDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // rentalBookingsDataGridViewTextBoxColumn
            // 
            rentalBookingsDataGridViewTextBoxColumn.DataPropertyName = "RentalBookings";
            rentalBookingsDataGridViewTextBoxColumn.HeaderText = "RentalBookings";
            rentalBookingsDataGridViewTextBoxColumn.Name = "rentalBookingsDataGridViewTextBoxColumn";
            rentalBookingsDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // CustomerProfilesView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(chkShowArchived);
            Controls.Add(dgvCustomers);
            Controls.Add(primaryButtonNewCustomer);
            Controls.Add(searchBar1);
            Controls.Add(label1);
            Name = "CustomerProfilesView";
            Padding = new Padding(30);
            Size = new Size(1712, 955);
            Load += CustomerProfilesView_Load;
            ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource1).EndInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)companyDatabaseBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)customerBindingSource2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Controls.PrimaryButton primaryButtonNewCustomer;
        private Controls.ModernDataGridView dgvCustomers;
        private BindingSource customerBindingSource;
        private BindingSource companyDatabaseBindingSource;
        private Controls.SearchBar searchBar1;
        private Controls.AtelierCheckBox chkShowArchived;
        private BindingSource customerBindingSource1;
        private BindingSource customerBindingSource2;
        private DataGridViewTextBoxColumn customerIdDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn customerCodeDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn companyIdDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn firstNameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn middleNameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn lastNameDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn contactNumberDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn emailAddressDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn addressDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn bustSizeDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn waistSizeDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn hipSizeDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn createdAtDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn rentalBookingsDataGridViewTextBoxColumn;
    }
}
