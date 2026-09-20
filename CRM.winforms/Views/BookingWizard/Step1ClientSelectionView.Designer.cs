namespace CRM.winforms.Views;

partial class Step1ClientSelectionView
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
        SuspendLayout();
        // 
        // Step1ClientSelectionView
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(249, 241, 241);
        Name = "Step1ClientSelectionView";
        Padding = new Padding(30);
        Size = new Size(1712, 955);
        Load += Step1ClientSelectionView_Load;
        ResumeLayout(false);
    }

    #endregion
    private Label label2;
    private Controls.SearchBar searchBar1;
    private Label label1;
}
