namespace StarTrekCCG;

/// <summary>How this window talks to the lobby. One match stays in one mode.</summary>
public sealed class LobbySignIn
{
    public string Mode { get; init; } = "sandbox";
    public string Name { get; init; } = "";
    public string? Token { get; init; }
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 7788;
    public int? Latinum { get; set; }

    public bool IsAccount => string.Equals(Mode, "account", System.StringComparison.Ordinal);

    public static LobbySignIn Sandbox(string? name = null) => new()
    {
        Mode = "sandbox",
        Name = name ?? ""
    };
}
