namespace StarTrekCCG;

/// <summary>
/// DEV-ONLY HOOK. One session switch for debug tools in the main menu and in the game.
/// Off again when the process restarts. Not written to the account database.
/// Later debug options check <see cref="IsEnabled"/>.
/// </summary>
public static class DeveloperMode
{
    public static bool IsEnabled { get; private set; } = true; // DEV-ONLY HOOK: starts on for local debugging. Remove before release.

    public static event Action? Changed;

    public static void Enable()
    {
        if (IsEnabled)
            return;
        IsEnabled = true;
        Changed?.Invoke();
    }
}
