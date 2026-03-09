using System.Diagnostics;

namespace CodingTimeTrackerForSteam.Core;

internal class EditorMonitor : IDisposable
{
    private static readonly HashSet<string> _editors =
    [
        "code", "devenv", "idea64", "pycharm64", "rider64", "clion64",
        "phpstorm64", "webstorm64", "studio64", "eclipse", "netbeans64",
        "codeblocks", "qtcreator", "kdevelop", "jdev", "monodevelop",
        "arduino", "sublime_text", "atom", "notepad++", "brackets",
        "geany", "kate", "gedit", "komodo", "jedit", "bbedit",
        "spyder", "thonny", "rstudio", "vim", "nvim", "emacs"
    ];

    private System.Threading.Timer? _timer;
    private bool _editorWasRunning;
    private readonly object _lock = new();

    public event Action? EditorStarted;
    public event Action? EditorStopped;

    public void Start()
    {
        _editorWasRunning = IsAnyRunning();
        _timer = new System.Threading.Timer(OnTick, null, 0, 3000);
    }

    private static bool IsAnyRunning() =>
        Process.GetProcesses().Any(p => _editors.Contains(p.ProcessName.ToLowerInvariant()));

    private void OnTick(object? _)
    {
        bool running = IsAnyRunning();

        lock (_lock)
        {
            if (running && !_editorWasRunning)
            {
                _editorWasRunning = true;
                EditorStarted?.Invoke();
            }
            else if (!running && _editorWasRunning)
            {
                _editorWasRunning = false;
                EditorStopped?.Invoke();
            }
        }
    }

    public void Dispose() => _timer?.Dispose();
}
