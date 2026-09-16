using System;
using System.Windows.Forms;
using CRM.winforms.Forms;

namespace CRM.winforms;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK && login.AuthenticatedUser != null)
        {
            Application.Run(new MainForm(login.AuthenticatedUser));
        }
    }
}