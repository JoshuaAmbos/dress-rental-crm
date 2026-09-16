using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM.winforms;

public class LoginResult
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public partial class LoginForm : Form
{
    // API Client
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("http://10.0.2.2:5171/") };

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private CheckBox chkShowPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;

    // Colors
    private static readonly Color ColorHeader = Color.FromArgb(38, 22, 24);
    private static readonly Color ColorPrimary = Color.FromArgb(114, 47, 55);
    private static readonly Color ColorContentBg = Color.FromArgb(249, 241, 241);
    private static readonly Color ColorCard = Color.White;

    public LoginResult? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        InitializeLoginUI();
    }

    private void InitializeLoginUI()
    {
        this.Text = "Dress Rental CRM - Secure Login";
        this.Size = new Size(420, 560);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = ColorContentBg;
        this.Font = new Font("Segoe UI", 9.5f);

        var pnlCard = new Panel
        {
            Width = 340,
            Height = 460,
            Left = (this.ClientSize.Width - 340) / 2,
            Top = (this.ClientSize.Height - 460) / 2,
            BackColor = ColorCard
        };

        var lblLogo = new Label
        {
            Text = "👗", // Placeholder icon 
            Font = new Font("Segoe UI Emoji", 36f),
            AutoSize = false,
            Width = 340,
            Height = 60,
            Top = 20,
            Left = 0,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = "Dress Rental CRM",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = ColorHeader,
            AutoSize = false,
            Width = 340,
            Height = 30,
            Top = 85,
            Left = 0,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSubtitle = new Label
        {
            Text = "Please log in to your account",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.DimGray,
            AutoSize = false,
            Width = 340,
            Height = 20,
            Top = 115,
            Left = 0,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblUser = new Label { Text = "Username", Left = 30, Top = 160, Width = 280, ForeColor = ColorHeader, Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) };
        txtUsername = new TextBox 
        { 
            Left = 30, Top = 185, Width = 280, 
            Font = new Font("Segoe UI", 11f), 
            Text = "superadmin",
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblPass = new Label { Text = "Password", Left = 30, Top = 230, Width = 280, ForeColor = ColorHeader, Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) };
        txtPassword = new TextBox 
        { 
            Left = 30, Top = 255, Width = 280, 
            Font = new Font("Segoe UI", 11f), 
            UseSystemPasswordChar = true, 
            Text = "SuperSecret123!",
            BorderStyle = BorderStyle.FixedSingle
        };

        chkShowPassword = new CheckBox
        {
            Text = "Show Password",
            Left = 30,
            Top = 290,
            Width = 150,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.DimGray,
            Cursor = Cursors.Hand
        };
        chkShowPassword.CheckedChanged += (s, e) => 
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        };

        lblStatus = new Label 
        { 
            Left = 30, Top = 330, Width = 280, Height = 40,
            ForeColor = Color.Firebrick, 
            TextAlign = ContentAlignment.TopCenter,
            Text = "" 
        };

        btnLogin = new Button
        {
            Text = "LOG IN",
            Left = 30,
            Top = 380,
            Width = 280,
            Height = 45,
            BackColor = ColorPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += async (s, e) => await AttemptLoginAsync();

        pnlCard.Controls.AddRange(new Control[] 
        { 
            lblLogo, lblTitle, lblSubtitle, 
            lblUser, txtUsername, 
            lblPass, txtPassword, chkShowPassword, 
            lblStatus, btnLogin 
        });

        this.Controls.Add(pnlCard);
        this.AcceptButton = btnLogin;
    }

    private async Task AttemptLoginAsync()
    {
        lblStatus.Text = "Authenticating...";
        lblStatus.ForeColor = Color.DimGray;
        btnLogin.Enabled = false;

        var payload = new
        {
            Username = txtUsername.Text.Trim(),
            Password = txtPassword.Text
        };

        try
        {
            var response = await Http.PostAsJsonAsync("/api/auth/login", payload);
            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                AuthenticatedUser = await response.Content.ReadFromJsonAsync<LoginResult>(options);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.ForeColor = Color.Firebrick;
                lblStatus.Text = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            lblStatus.ForeColor = Color.Firebrick;
            lblStatus.Text = $"Connection failed.\nIs the API running?";
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }
}