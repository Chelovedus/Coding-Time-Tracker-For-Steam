using System.Diagnostics;
using CodingTimeTrackerForSteam.Localization;
using CodingTimeTrackerForSteam.Platform;

namespace CodingTimeTrackerForSteam.Steam;

internal class GameManager
{
    private const string GameSteamUri    = "steam://rungameid/779260";
    private const string GameStoreUrl    = "https://store.steampowered.com/app/779260";
    private const string GameProcessName = "Kode Studio";

    private readonly HashSet<uint>  _gamePids = [];
    private readonly object         _lock     = new();

    private NativeMethods.WinEventDelegate? _winEventProc;
    private IntPtr _hookShow;
    private IntPtr _hookFg;
    private System.Threading.Timer? _hideTimer;
    private int _hideTickCount;
    private bool _suspended;

    public bool IsRunning() =>
        Process.GetProcessesByName(GameProcessName).Any();

    public void Launch()
    {
        try
        {
            InstallEventHook();
            Process.Start(new ProcessStartInfo(GameSteamUri) { UseShellExecute = true });
            Console.WriteLine("Game launched via Steam.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting the game: {ex.Message}");
        }
    }

    public void Close()
    {
        StopHideTimer();
        UninstallEventHook();
        ResumeGameProcesses();

        foreach (var p in Process.GetProcessesByName(GameProcessName))
        {
            using (p)
            {
                try
                {
                    p.Kill();
                    Console.WriteLine($"Game \"{GameProcessName}\" closed (PID {p.Id}).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not kill game process: {ex.Message}");
                }
            }
        }

        lock (_lock)
            _gamePids.Clear();
    }

    public bool WaitForLaunch(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        int tick = 0;
        long lastAttempt = 0;
        const int retryInterval = 60_000;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (IsRunning())
            {
                Console.WriteLine("Kode Studio launched successfully.");
                RefreshGamePids();
                HideAllGameWindows();
                StartHideTimer();
                return true;
            }

            if (sw.ElapsedMilliseconds - lastAttempt >= retryInterval)
            {
                Console.WriteLine("Retrying Kode Studio launch via Steam URI...");
                Launch();
                lastAttempt = sw.ElapsedMilliseconds;
            }

            Thread.Sleep(500);

            if (++tick % 10 == 0)
                Console.WriteLine($"Waiting for Kode Studio... ({sw.ElapsedMilliseconds / 1000}s)");
        }

        return false;
    }

    public void ShowNotInstalledAndExit()
    {
        var locale = Localizer.Get();
        MessageBox.Show(locale.InstallMessage, locale.InstallTitle,
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Process.Start(new ProcessStartInfo(GameStoreUrl) { UseShellExecute = true });
        Application.Exit();
    }

    private void StartHideTimer()
    {
        _hideTickCount = 0;
        _hideTimer ??= new System.Threading.Timer(_ =>
        {
            HideAllGameWindows();
            if (Interlocked.Increment(ref _hideTickCount) >= 50)
            {
                StopHideTimer();
                SuspendGameProcesses();
            }
        }, null, 0, 100);
    }

    private void StopHideTimer()
    {
        _hideTimer?.Dispose();
        _hideTimer = null;
    }

    private void SuspendGameProcesses()
    {
        if (_suspended) return;
        foreach (var p in Process.GetProcessesByName(GameProcessName))
        {
            using (p)
            {
                try
                {
                    NativeMethods.NtSuspendProcess(p.Handle);
                    NativeMethods.EmptyWorkingSet(p.Handle);
                    Console.WriteLine($"Game process suspended and memory trimmed (PID {p.Id}).");
                }
                catch { }
            }
        }
        _suspended = true;
    }

    private void ResumeGameProcesses()
    {
        if (!_suspended) return;
        foreach (var p in Process.GetProcessesByName(GameProcessName))
        {
            using (p)
            {
                try
                {
                    NativeMethods.NtResumeProcess(p.Handle);
                    Console.WriteLine($"Game process resumed (PID {p.Id}).");
                }
                catch { }
            }
        }
        _suspended = false;
    }

    private void InstallEventHook()
    {
        if (_hookShow != IntPtr.Zero) return;

        _winEventProc = OnWinEvent;

        _hookShow = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_SHOW, NativeMethods.EVENT_OBJECT_SHOW,
            IntPtr.Zero, _winEventProc, 0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        _hookFg = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _winEventProc, 0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS);

        Console.WriteLine("WinEvent hooks installed.");
    }

    private void UninstallEventHook()
    {
        if (_hookShow != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hookShow);
            _hookShow = IntPtr.Zero;
        }
        if (_hookFg != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hookFg);
            _hookFg = IntPtr.Zero;
        }
        _winEventProc = null;
        Console.WriteLine("WinEvent hooks removed.");
    }

    private void OnWinEvent(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;
        if (idObject != 0) return;

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);

        bool isGamePid;
        lock (_lock)
        {
            if (!_gamePids.Contains(pid))
            {
                RefreshGamePids();
                isGamePid = _gamePids.Contains(pid);
            }
            else
            {
                isGamePid = true;
            }
        }

        if (!isGamePid) return;

        HideWindow(hwnd, pid);
    }

    private void RefreshGamePids()
    {
        lock (_lock)
        {
            _gamePids.Clear();
            foreach (var p in Process.GetProcessesByName(GameProcessName))
            {
                using (p)
                    _gamePids.Add((uint)p.Id);
            }
        }
    }

    private static void HideWindow(IntPtr hwnd, uint pid)
    {
        if (!NativeMethods.IsWindowVisible(hwnd)) return;

        int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        int newStyle = (exStyle | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW;
        if (newStyle != exStyle)
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, newStyle);

        NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
        Console.WriteLine($"Game window hidden (PID {pid}).");
    }

    private void HideAllGameWindows()
    {
        RefreshGamePids();

        HashSet<uint> pids;
        lock (_lock)
            pids = [.. _gamePids];

        if (pids.Count == 0) return;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint windowPid);
            if (pids.Contains(windowPid))
                HideWindow(hWnd, windowPid);
            return true;
        }, IntPtr.Zero);
    }
}
