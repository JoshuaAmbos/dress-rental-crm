using System;
using System.Windows.Forms;
using CRM.winforms.Models;

namespace CRM.winforms.Views;

public partial class ClientsView : UserControl
{
    private readonly Func<int> _getCompanyId;
    private readonly LoginResult _user;

    // Parameterless constructor for the Visual Studio Designer
    public ClientsView() : this(() => 1, new LoginResult { Username = "Designer" })
    {
    }

    // Runtime constructor invoked by MainForm
    public ClientsView(Func<int> getCompanyId, LoginResult user)
    {
        _getCompanyId = getCompanyId ?? throw new ArgumentNullException(nameof(getCompanyId));
        _user = user ?? throw new ArgumentNullException(nameof(user));

        this.Dock = DockStyle.Fill;
    }

    public void ReloadData()
    {
        // Stub for future data fetching/refresh logic
    }
}