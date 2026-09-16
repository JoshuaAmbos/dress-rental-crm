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
    private static readonly HttpClient Http = new() { BaseAddress = new Uri("http://localhost:5171/") };

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Label lblStatus = null!;

    public LoginResult? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        InitializeLoginUI();
    }

    private void InitializeLoginUI()
    {
        this.Text = "Dress Rental CRM - Login";
        this.Size = new Size(380, 260);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var lblUser = new Label { Text = "Username:", Left = 30, Top = 30, Width = 80 };
        txtUsername = new TextBox { Left = 120, Top = 27, Width = 200, Text = "superadmin" };

        var lblPass = new Label { Text = "Password:", Left = 30, Top = 70, Width = 80 };
        txtPassword = new TextBox { Left = 120, Top = 67, Width = 200, UseSystemPasswordChar = true, Text = "SuperSecret123!" };

        lblStatus = new Label { Left = 30, Top = 105, Width = 290, ForeColor = Color.Firebrick, Text = "" };

        btnLogin = new Button
        {
            Text = "Log In",
            Left = 120,
            Top = 140,
            Width = 200,
            Height = 36,
            BackColor = Color.FromArgb(114, 47, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += async (s, e) => await AttemptLoginAsync();

        this.AcceptButton = btnLogin;
        this.Controls.AddRange(new Control[] { lblUser, txtUsername, lblPass, txtPassword, lblStatus, btnLogin });
    }

    private async Task AttemptLoginAsync()
    {
        lblStatus.Text = "";
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
                lblStatus.Text = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Connection failed: {ex.Message}";
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }
}