using System.Text.Json;
using System.Windows;
using StarTrekCCG.Network;

namespace StarTrekCCG;

public partial class AccountGateWindow : Window
{
    public LobbySignIn Result { get; private set; } = LobbySignIn.Sandbox();

    public AccountGateWindow()
    {
        InitializeComponent();
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
        => await AuthAsync(register: true).ConfigureAwait(true);

    private async void Login_Click(object sender, RoutedEventArgs e)
        => await AuthAsync(register: false).ConfigureAwait(true);

    private void Sandbox_Click(object sender, RoutedEventArgs e)
    {
        if (!TryEndpoint(out var host, out var port))
            return;
        var name = (NameBox.Text ?? "").Trim();
        if (name.Length > 24)
        {
            StatusText.Text = "Display name is max 24.";
            return;
        }
        Result = new LobbySignIn
        {
            Mode = "sandbox",
            Name = name,
            Host = host,
            Port = port
        };
        DialogResult = true;
    }

    private async System.Threading.Tasks.Task AuthAsync(bool register)
    {
        if (!TryEndpoint(out var host, out var port))
            return;
        var name = (NameBox.Text ?? "").Trim();
        var password = PasswordBox.Password ?? "";
        if (name.Length == 0 || name.Length > 24)
        {
            StatusText.Text = "Display name is max 24.";
            return;
        }
        StatusText.Text = register ? "Registering..." : "Logging in...";
        var mm = new MatchmakingClient();
        try
        {
            await mm.ConnectSocketAsync(host, port).ConfigureAwait(true);
            var reply = register
                ? await mm.RegisterAsync(name, password).ConfigureAwait(true)
                : await mm.LoginAsync(name, password).ConfigureAwait(true);
            var type = reply.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : "";
            if (!string.Equals(type, "auth", System.StringComparison.Ordinal))
            {
                var message = reply.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String
                    ? msg.GetString()
                    : "Sign-in failed.";
                StatusText.Text = message ?? "Sign-in failed.";
                return;
            }
            var token = reply.TryGetProperty("token", out var tokenEl) ? tokenEl.GetString() : "";
            var bound = reply.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : name;
            if (string.IsNullOrWhiteSpace(token))
            {
                StatusText.Text = "The service did not issue a token.";
                return;
            }
            int? latinum = null;
            if (reply.TryGetProperty("latinum", out var latEl) && latEl.TryGetInt32(out var lat))
                latinum = lat;
            Result = new LobbySignIn
            {
                Mode = "account",
                Name = bound ?? name,
                Token = token,
                Host = host,
                Port = port,
                Latinum = latinum
            };
            DialogResult = true;
        }
        catch (System.Exception ex)
        {
            StatusText.Text = "Sign-in failed: " + ex.Message;
        }
        finally
        {
            mm.Dispose();
        }
    }

    private bool TryEndpoint(out string host, out int port)
    {
        host = "";
        port = 0;
        var text = (ServiceBox.Text ?? "").Trim();
        var colon = text.LastIndexOf(':');
        if (colon <= 0 || !int.TryParse(text[(colon + 1)..], out port) || port is < 1 or > 65535)
        {
            StatusText.Text = "Server address must be host:port, for example 127.0.0.1:7788.";
            return false;
        }
        host = text[..colon].Trim();
        if (host.Length == 0)
        {
            StatusText.Text = "Server address must be host:port, for example 127.0.0.1:7788.";
            return false;
        }
        return true;
    }
}
