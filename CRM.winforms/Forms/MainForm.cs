using CRM.winforms.Models;

namespace CRM.winforms
{
    internal class MainForm : Form
    {
        private LoginResult authenticatedUser;

        public MainForm(LoginResult authenticatedUser)
        {
            this.authenticatedUser = authenticatedUser;
        }
    }
}