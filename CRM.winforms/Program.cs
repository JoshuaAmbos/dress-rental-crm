using CRM.winforms.Forms;

namespace CRM.winforms;

static class Program
{
    [STAThread]
    static void Main()
    {
        DotNetEnv.Env.TraversePath().Load();
        ApplicationConfiguration.Initialize();

        while (true)
        {
            using var loginForm = new LoginForm();
            if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser == null)
            {
                break;
            }

            using var mainForm = new MainForm(loginForm.AuthenticatedUser);
            Application.Run(mainForm);

            if (!mainForm.IsSignedOut)
            {
                break;
            }
        }
    }
}