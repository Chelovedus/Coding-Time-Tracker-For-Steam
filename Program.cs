// Program.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodingTimeTrackerForSteam.Linux
{
    internal sealed class Program
    {
        // Configuration (change if needed)
        private const string GameProcessName = "kodestudio";
        private const string SteamProcessName = "steam";
        private const string SteamWebHelperProcessName = "steamwebhelper";
        private const int EditorCheckIntervalMs = 1000;
        private const int WaitForSteamTimeoutMs = 180_000;
        private const int WaitForGameLaunchTimeoutMs = 300_000;
        private const int GameLaunchAttemptIntervalMs = 60_000;
        private const string SteamUri = "steam://rungameid/779260";
        private static readonly string[] CodeEditors =
        {
            "code", "devenv", "idea64", "pycharm64", "rider64", "clion64",
            "phpstorm64", "webstorm64", "studio64", "eclipse", "netbeans64",
            "codeblocks", "qtcreator", "kdevelop", "jdev", "monodevelop",
            "arduino", "sublime_text", "atom", "notepad++", "brackets",
            "geany", "kate", "gedit", "komodo", "jedit", "bbedit",
            "spyder", "thonny", "rstudio", "vim", "nvim", "emacs"
        }.Select(s => s.ToLowerInvariant()).ToArray();

        private static Process _gameProcess;
        private static bool _isReady;
        private static CancellationTokenSource _cts = new();

        private static async Task<int> Main()
        {
            Console.WriteLine("Coding Time Tracker for Steam (Linux) starting...");

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutdown requested (Ctrl+C).");
                e.Cancel = true;
                _cts.Cancel();
            };

            // Try to ensure Steam is running
            if (!IsProcessRunning(SteamProcessName))
            {
                StartSteam();
            }

            await WaitForSteamReadyAsync(WaitForSteamTimeoutMs, _cts.Token)
                .ContinueWith(t => { /* log or ignore timeout */ });

            // Start the game (Kode Studio) via steam uri
            TryStartGameViaSteamUri();

            // Wait for game launch, attempts repeated
            await WaitForGameLaunchAsync(WaitForGameLaunchTimeoutMs, _cts.Token);

            // Periodically check editors and manage the game
            try
            {
                await RunEditorWatcherAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }

            // Cleanup on exit
            Console.WriteLine("Exiting: cleaning up.");
            TryKillGameProcess();
            return 0;
        }

        private static void StartSteam()
        {
            try
            {
                Console.WriteLine("Starting Steam via xdg-open...");
                // Use xdg-open to open steam:// or run steam directly
                StartProcessDetached("xdg-open", SteamUri);
                Task.Delay(5000).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Steam: {ex.Message}");
                Notify($"Failed to start Steam: {ex.Message}");
            }
        }

        private static void TryStartGameViaSteamUri()
        {
            try
            {
                Console.WriteLine("Attempting to start Kode Studio via steam URI (xdg-open).");
                StartProcessDetached("xdg-open", SteamUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start game via URI: {ex.Message}");
            }
        }

        private static async Task WaitForSteamReadyAsync(int timeoutMs, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (IsProcessRunning(SteamProcessName) && IsProcessRunning(SteamWebHelperProcessName))
                {
                    Console.WriteLine("Steam appears ready (steam + steamwebhelper running).");
                    return;
                }

                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                await Task.Delay(1000, ct);
            }
            Console.WriteLine($"WaitForSteamReady timeout after {timeoutMs}ms; continuing anyway.");
        }

        private static async Task WaitForGameLaunchAsync(int timeoutMs, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            long lastAttempt = 0;
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (IsGameRunning())
                {
                    _isReady = true;
                    Console.WriteLine("Kode Studio process detected.");
                    Notify("Kode Studio launched and detected.");
                    return;
                }

                if (sw.ElapsedMilliseconds - lastAttempt >= GameLaunchAttemptIntervalMs)
                {
                    TryStartGameViaSteamUri();
                    lastAttempt = sw.ElapsedMilliseconds;
                }

                if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                await Task.Delay(500, ct);
            }

            // timeout: inform user via console/notification and open store page (xdg-open)
            Console.WriteLine("Timeout waiting for Kode Studio to launch.");
            Notify("Kode Studio did not launch within timeout. Please install or check Steam.");
            StartProcessDetached("xdg-open", "https://store.steampowered.com/app/779260");
            // We purposely don't exit; still continue in case user starts the editor later.
        }

        private static async Task RunEditorWatcherAsync(CancellationToken ct)
        {
            Console.WriteLine("Starting editor watcher loop.");

            while (!ct.IsCancellationRequested)
            {
                // Try to hide window if present (best-effort)
                TryHideGameWindow();

                if (_isReady)
                {
                    if (IsAnyCodeEditorRunning())
                    {
                        // ensure game is running
                        if (!IsGameRunning())
                        {
                            TryStartGameViaSteamUri();
                            Console.WriteLine("Editor detected but game not running -> starting game.");
                        }
                    }
                    else
                    {
                        // No editor running: close game
                        Console.WriteLine("No code editor detected -> closing game.");
                        TryCloseGameWindow();
                        _isReady = false;
                    }
                }
                else
                {
                    // If not ready but _gameProcess exists and exited, log
                    if (_gameProcess != null && _gameProcess.HasExited)
                    {
                        Console.WriteLine("Game process exited.");
                        _gameProcess = null;
                    }
                }

                await Task.Delay(EditorCheckIntervalMs, ct);
            }
        }

        #region Helpers: process checks, start/kill, editors

        private static bool IsProcessRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return false;
            try
            {
                return Process.GetProcessesByName(processName).Any();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAnyCodeEditorRunning()
        {
            try
            {
                var procs = Process.GetProcesses();
                foreach (var p in procs)
                {
                    try
                    {
                        var name = p.ProcessName?.ToLowerInvariant();
                        if (string.IsNullOrEmpty(name)) continue;
                        if (CodeEditors.Contains(name)) return true;
                    }
                    catch { /* ignore processes we can't query */ }
                }
            }
            catch { /* ignore */ }
            return false;
        }

        private static bool IsGameRunning()
        {
            return IsProcessRunning(GameProcessName);
        }

        private static void TryKillGameProcess()
        {
            try
            {
                var procs = Process.GetProcessesByName(GameProcessName);
                foreach (var p in procs)
                {
                    try
                    {
                        Console.WriteLine($"Killing process {p.Id} ({p.ProcessName}).");
                        p.Kill(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill process {p.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error killing game processes: {ex.Message}");
            }
        }

        private static void TryCloseGameWindow()
        {
            if (TryCloseByWmctrl() || TryCloseByXdotool())
            {
                Console.WriteLine("Requested window close via window tool.");
                Notify("Kode Studio closed (requested).");
                return;
            }

            // fallback: kill process
            TryKillGameProcess();
        }

        private static void TryHideGameWindow()
        {
            if (TryHideByWmctrl() || TryHideByXdotool())
            {
                Console.WriteLine("Requested hide via window tool.");
            }
            // else nothing (best-effort)
        }

        #endregion

        #region Helpers: external tool wrappers (wmctrl/xdotool/notify-send)

        private static bool IsExecutableOnPath(string name)
        {
            try
            {
                var psi = new ProcessStartInfo("which", name)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                p.WaitForExit(2000);
                var outp = p.StandardOutput.ReadToEnd();
                return !string.IsNullOrWhiteSpace(outp);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryHideByWmctrl()
        {
            if (!IsExecutableOnPath("wmctrl")) return false;
            try
            {
                // wmctrl -r "Kode Studio" -b add,hidden
                StartProcessDetached("wmctrl", $"-r \"Kode Studio\" -b add,hidden");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"wmctrl hide failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryHideByXdotool()
        {
            if (!IsExecutableOnPath("xdotool")) return false;
            try
            {
                // search windows by name then windowunmap
                StartProcessDetached("bash", $"-c \"xdotool search --name 'Kode Studio' 2>/dev/null | xargs -r -I{{}} xdotool windowunmap {{}}\"");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"xdotool hide failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryCloseByWmctrl()
        {
            if (!IsExecutableOnPath("wmctrl")) return false;
            try
            {
                // wmctrl -c "Kode Studio"
                StartProcessDetached("wmctrl", $"-c \"Kode Studio\"");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"wmctrl close failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryCloseByXdotool()
        {
            if (!IsExecutableOnPath("xdotool")) return false;
            try
            {
                // search windows by name then windowkill
                StartProcessDetached("bash", $"-c \"xdotool search --name 'Kode Studio' 2>/dev/null | xargs -r -I{{}} xdotool windowkill {{}}\"");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"xdotool close failed: {ex.Message}");
                return false;
            }
        }

        private static void Notify(string message)
        {
            // Best-effort desktop notification via notify-send; otherwise console
            if (IsExecutableOnPath("notify-send"))
            {
                try
                {
                    StartProcessDetached("notify-send", $"\"Coding Time Tracker\" \"{EscapeArg(message)}\"");
                }
                catch
                {
                    Console.WriteLine("notify-send failed; message: " + message);
                }
            }
            else
            {
                Console.WriteLine("NOTIFY: " + message);
            }
        }

        #endregion

        #region Process start helpers

        private static void StartProcessDetached(string fileName, string arguments)
        {
            // Start external helper commands in detached manner, not waiting for them.
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            // For some commands (like xdg-open) it's fine to run and return quickly.
            try
            {
                using var proc = Process.Start(psi);
                // do not wait; let it spawn
            }
            catch (Exception ex)
            {
                // On some systems xdg-open requires shell. Try fallback.
                Console.WriteLine($"StartProcessDetached failed for {fileName} {arguments}: {ex.Message}");
                throw;
            }
        }

        private static string EscapeArg(string s)
        {
            return s?.Replace("\"", "\\\"") ?? string.Empty;
        }

        #endregion
    }
}