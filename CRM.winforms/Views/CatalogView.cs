using CRM.winforms.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CRM.winforms.Views
{
    public partial class CatalogView : UserControl
    {
        public CatalogView()
        {
            InitializeComponent();

            searchBar1.SetCueBanner("Search by garment name, SKU, or category...");
        }

        private void secondaryButtonAll_Click(object sender, EventArgs e)
        {

        }
    }
}
