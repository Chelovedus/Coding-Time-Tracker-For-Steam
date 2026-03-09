using CodingTimeTrackerForSteam.Steam;
using CodingTimeTrackerForSteam.UI;

namespace CodingTimeTrackerForSteam.Core;

internal class AppController : IDisposable
{
    private readonly SteamManager  _steam   = new();
    private readonly GameManager   _game    = new();
    private readonly EditorMonitor _monitor = new();
    private TrayIconManager?       _tray;

    public void Start()
    {
        _tray = new TrayIconManager();
        _tray.ExitRequested += OnExit;

        if (!_steam.IsRunning())
            _steam.Start();

        _steam.WaitUntilReady(timeoutMs: 180_000);

        _game.Launch();
        bool launched = _game.WaitForLaunch(timeoutMs: 300_000);

        if (!launched)
        {
            _game.ShowNotInstalledAndExit();
            return;
        }

        _monitor.EditorStarted += OnEditorStarted;
        _monitor.EditorStopped += OnEditorStopped;
        _monitor.Start();
    }

    private void OnEditorStarted()
    {
        if (!_game.IsRunning())
            _game.Launch();
    }

    private void OnEditorStopped()
    {
        _game.Close();
        Console.WriteLine("Editor closed — game stopped.");
    }

    private void OnExit()
    {
        Dispose();
        Application.Exit();
    }

    public void Dispose()
    {
        _monitor.Dispose();
        _game.Close();
        _tray?.Dispose();
    }
}
