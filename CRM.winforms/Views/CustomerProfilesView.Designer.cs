namespace CRM.winforms.Views;

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
        customerBindingSource1 = new BindingSource(components);
        customerBindingSource = new BindingSource(components);
        companyDatabaseBindingSource = new BindingSource(components);
        chkShowArchived = new CRM.winforms.Controls.AtelierCheckBox();
        customerBindingSource2 = new BindingSource(components);
        dgvCustomers = new DataGridView();
        customerCodeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        companyIdDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        fullNameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        contactNumberDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        emailAddressDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        addressDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        bustSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        waistSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        hipSizeDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        createdAtDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        rentalBookingsDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
        customerBindingSource3 = new BindingSource(components);
        searchBar1 = new CRM.winforms.Controls.SearchBar();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource1).BeginInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource).BeginInit();
        ((System.ComponentModel.ISupportInitialize)companyDatabaseBindingSource).BeginInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource2).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).BeginInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource3).BeginInit();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Font = new Font("Segoe UI", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
        label1.ForeColor = Color.FromArgb(38, 22, 24);
        label1.Location = new Point(33, 28);
        label1.Name = "label1";
        label1.Size = new Size(153, 37);
        label1.TabIndex = 0;
        label1.Text = "Customers";
        // 
        // primaryButtonNewCustomer
        // 
        primaryButtonNewCustomer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        primaryButtonNewCustomer.BackColor = Color.FromArgb(190, 110, 120);
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
        // chkShowArchived
        // 
        chkShowArchived.BackColor = Color.Transparent;
        chkShowArchived.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        chkShowArchived.ForeColor = Color.FromArgb(45, 35, 40);
        chkShowArchived.Location = new Point(359, 93);
        chkShowArchived.Name = "chkShowArchived";
        chkShowArchived.Size = new Size(150, 34);
        chkShowArchived.TabIndex = 5;
        chkShowArchived.Text = "Show Archived";
        chkShowArchived.UseVisualStyleBackColor = false;
        chkShowArchived.CheckedChanged += ChkShowArchived_CheckedChanged;
        // 
        // customerBindingSource2
        // 
        customerBindingSource2.DataSource = typeof(domain.entities.Customer);
        // 
        // dgvCustomers
        // 
        dgvCustomers.AllowUserToAddRows = false;
        dgvCustomers.AllowUserToDeleteRows = false;
        dgvCustomers.AllowUserToResizeRows = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(250, 244, 244);
        dataGridViewCellStyle1.ForeColor = Color.FromArgb(44, 34, 38);
        dataGridViewCellStyle1.Padding = new Padding(12, 2, 12, 2);
        dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(190, 110, 120);
        dataGridViewCellStyle1.SelectionForeColor = Color.White;
        dgvCustomers.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        dgvCustomers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgvCustomers.AutoGenerateColumns = false;
        dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCustomers.BackgroundColor = Color.White;
        dgvCustomers.BorderStyle = BorderStyle.Fixed3D;
        dgvCustomers.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvCustomers.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(249, 241, 241);
        dataGridViewCellStyle2.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(38, 22, 24);
        dataGridViewCellStyle2.Padding = new Padding(12, 0, 12, 0);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(249, 241, 241);
        dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(38, 22, 24);
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        dgvCustomers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        dgvCustomers.ColumnHeadersHeight = 44;
        dgvCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvCustomers.Columns.AddRange(new DataGridViewColumn[] { customerCodeDataGridViewTextBoxColumn, companyIdDataGridViewTextBoxColumn, fullNameDataGridViewTextBoxColumn, contactNumberDataGridViewTextBoxColumn, emailAddressDataGridViewTextBoxColumn, addressDataGridViewTextBoxColumn, bustSizeDataGridViewTextBoxColumn, waistSizeDataGridViewTextBoxColumn, hipSizeDataGridViewTextBoxColumn, createdAtDataGridViewTextBoxColumn, rentalBookingsDataGridViewTextBoxColumn });
        dgvCustomers.DataSource = customerBindingSource3;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(44, 34, 38);
        dataGridViewCellStyle3.Padding = new Padding(12, 2, 12, 2);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(190, 110, 120);
        dataGridViewCellStyle3.SelectionForeColor = Color.White;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        dgvCustomers.DefaultCellStyle = dataGridViewCellStyle3;
        dgvCustomers.EnableHeadersVisualStyles = false;
        dgvCustomers.GridColor = Color.FromArgb(234, 223, 217);
        dgvCustomers.Location = new Point(33, 159);
        dgvCustomers.MultiSelect = false;
        dgvCustomers.Name = "dgvCustomers";
        dgvCustomers.ReadOnly = true;
        dgvCustomers.RowHeadersVisible = false;
        dgvCustomers.RowTemplate.Height = 40;
        dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvCustomers.Size = new Size(1646, 763);
        dgvCustomers.TabIndex = 6;
        // 
        // customerCodeDataGridViewTextBoxColumn
        // 
        customerCodeDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        customerCodeDataGridViewTextBoxColumn.DataPropertyName = "CustomerCode";
        customerCodeDataGridViewTextBoxColumn.FillWeight = 85F;
        customerCodeDataGridViewTextBoxColumn.HeaderText = "Client Code";
        customerCodeDataGridViewTextBoxColumn.Name = "customerCodeDataGridViewTextBoxColumn";
        customerCodeDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // companyIdDataGridViewTextBoxColumn
        // 
        companyIdDataGridViewTextBoxColumn.DataPropertyName = "CompanyId";
        companyIdDataGridViewTextBoxColumn.HeaderText = "Company";
        companyIdDataGridViewTextBoxColumn.Name = "companyIdDataGridViewTextBoxColumn";
        companyIdDataGridViewTextBoxColumn.ReadOnly = true;
        companyIdDataGridViewTextBoxColumn.Visible = false;
        // 
        // fullNameDataGridViewTextBoxColumn
        // 
        fullNameDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        fullNameDataGridViewTextBoxColumn.DataPropertyName = "FullName";
        fullNameDataGridViewTextBoxColumn.FillWeight = 140F;
        fullNameDataGridViewTextBoxColumn.HeaderText = "Client Name";
        fullNameDataGridViewTextBoxColumn.Name = "fullNameDataGridViewTextBoxColumn";
        fullNameDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // contactNumberDataGridViewTextBoxColumn
        // 
        contactNumberDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        contactNumberDataGridViewTextBoxColumn.DataPropertyName = "ContactNumber";
        contactNumberDataGridViewTextBoxColumn.HeaderText = "Phone";
        contactNumberDataGridViewTextBoxColumn.Name = "contactNumberDataGridViewTextBoxColumn";
        contactNumberDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // emailAddressDataGridViewTextBoxColumn
        // 
        emailAddressDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        emailAddressDataGridViewTextBoxColumn.DataPropertyName = "EmailAddress";
        emailAddressDataGridViewTextBoxColumn.FillWeight = 140F;
        emailAddressDataGridViewTextBoxColumn.HeaderText = "Email Address";
        emailAddressDataGridViewTextBoxColumn.Name = "emailAddressDataGridViewTextBoxColumn";
        emailAddressDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // addressDataGridViewTextBoxColumn
        // 
        addressDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        addressDataGridViewTextBoxColumn.DataPropertyName = "Address";
        addressDataGridViewTextBoxColumn.FillWeight = 120F;
        addressDataGridViewTextBoxColumn.HeaderText = "City / Address";
        addressDataGridViewTextBoxColumn.Name = "addressDataGridViewTextBoxColumn";
        addressDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // bustSizeDataGridViewTextBoxColumn
        // 
        bustSizeDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        bustSizeDataGridViewTextBoxColumn.DataPropertyName = "BustSize";
        bustSizeDataGridViewTextBoxColumn.FillWeight = 75F;
        bustSizeDataGridViewTextBoxColumn.HeaderText = "Bust (in)";
        bustSizeDataGridViewTextBoxColumn.Name = "bustSizeDataGridViewTextBoxColumn";
        bustSizeDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // waistSizeDataGridViewTextBoxColumn
        // 
        waistSizeDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        waistSizeDataGridViewTextBoxColumn.DataPropertyName = "WaistSize";
        waistSizeDataGridViewTextBoxColumn.FillWeight = 75F;
        waistSizeDataGridViewTextBoxColumn.HeaderText = "Waist (in)";
        waistSizeDataGridViewTextBoxColumn.Name = "waistSizeDataGridViewTextBoxColumn";
        waistSizeDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // hipSizeDataGridViewTextBoxColumn
        // 
        hipSizeDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        hipSizeDataGridViewTextBoxColumn.DataPropertyName = "HipSize";
        hipSizeDataGridViewTextBoxColumn.FillWeight = 75F;
        hipSizeDataGridViewTextBoxColumn.HeaderText = "Hips (in)";
        hipSizeDataGridViewTextBoxColumn.Name = "hipSizeDataGridViewTextBoxColumn";
        hipSizeDataGridViewTextBoxColumn.ReadOnly = true;
        // 
        // createdAtDataGridViewTextBoxColumn
        // 
        createdAtDataGridViewTextBoxColumn.DataPropertyName = "CreatedAt";
        createdAtDataGridViewTextBoxColumn.HeaderText = "CreatedAt";
        createdAtDataGridViewTextBoxColumn.Name = "createdAtDataGridViewTextBoxColumn";
        createdAtDataGridViewTextBoxColumn.ReadOnly = true;
        createdAtDataGridViewTextBoxColumn.Visible = false;
        // 
        // rentalBookingsDataGridViewTextBoxColumn
        // 
        rentalBookingsDataGridViewTextBoxColumn.DataPropertyName = "RentalBookings";
        rentalBookingsDataGridViewTextBoxColumn.HeaderText = "RentalBookings";
        rentalBookingsDataGridViewTextBoxColumn.Name = "rentalBookingsDataGridViewTextBoxColumn";
        rentalBookingsDataGridViewTextBoxColumn.ReadOnly = true;
        rentalBookingsDataGridViewTextBoxColumn.Visible = false;
        // 
        // customerBindingSource3
        // 
        customerBindingSource3.DataSource = typeof(domain.entities.Customer);
        // 
        // searchBar1
        // 
        searchBar1.BackColor = Color.Transparent;
        searchBar1.Location = new Point(33, 93);
        searchBar1.Name = "searchBar1";
        searchBar1.Size = new Size(320, 34);
        searchBar1.TabIndex = 7;
        // 
        // CustomerProfilesView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Controls.Add(searchBar1);
        Controls.Add(dgvCustomers);
        Controls.Add(chkShowArchived);
        Controls.Add(primaryButtonNewCustomer);
        Controls.Add(label1);
        Name = "CustomerProfilesView";
        Padding = new Padding(30);
        Size = new Size(1712, 955);
        Load += CustomerProfilesView_Load;
        ((System.ComponentModel.ISupportInitialize)customerBindingSource1).EndInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource).EndInit();
        ((System.ComponentModel.ISupportInitialize)companyDatabaseBindingSource).EndInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource2).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvCustomers).EndInit();
        ((System.ComponentModel.ISupportInitialize)customerBindingSource3).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Controls.PrimaryButton primaryButtonNewCustomer;
    private BindingSource customerBindingSource;
    private BindingSource companyDatabaseBindingSource;
    private Controls.AtelierCheckBox chkShowArchived;
    private BindingSource customerBindingSource1;
    private BindingSource customerBindingSource2;
    private DataGridView dgvCustomers;
    private BindingSource customerBindingSource3;
    private DataGridViewTextBoxColumn customerCodeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn companyIdDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn fullNameDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn contactNumberDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn emailAddressDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn addressDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn bustSizeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn waistSizeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn hipSizeDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn createdAtDataGridViewTextBoxColumn;
    private DataGridViewTextBoxColumn rentalBookingsDataGridViewTextBoxColumn;
    private Controls.SearchBar searchBar1;
}