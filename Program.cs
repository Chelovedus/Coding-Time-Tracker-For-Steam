using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodingTimeTrackerForSteam.Linux
{
    internal sealed class Program
    {
        private const string GameProcessName = "kodestudio";
        private const string SteamProcessName = "steam";
        private const string SteamWebHelperProcessName = "steamwebhelper";

        private const int EditorCheckIntervalMs = 1000;
        private const int WaitForSteamTimeoutMs = 300000;

        private const string SteamUri = "steam://rungameid/779260";
        private const string StoreUrl = "https://store.steampowered.com/app/779260/Kode_Studio/";

        private static readonly string[] CodeEditors =
        {
            "code","idea","pycharm","rider","clion","phpstorm","webstorm",
            "eclipse","netbeans","codeblocks","qtcreator","kdevelop",
            "monodevelop","arduino","sublime_text","atom","brackets",
            "geany","kate","gedit","komodo","jedit","spyder","thonny",
            "rstudio","vim","nvim","emacs","mousepad","pluma",
            "leafpad","micro"
        };

        private static CancellationTokenSource _cts = new();

        private static string SteamRoot = "";
        private static string SteamApps = "";
        private static string GameDir = "";
        private static string StubPath = "";

        private static bool _launchRequested = false;
        private static DateTime _lastLaunch = DateTime.MinValue;

        private static async Task<int> Main()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string installedBinary = Path.Combine(home, ".local/bin/CodingTimeTrackerForSteam");

            string current = Environment.ProcessPath;

            if (string.IsNullOrEmpty(current))
                return 1;

            bool isInstalledLocation = current == installedBinary;

            if (!isInstalledLocation)
            {
                InstallBinary();
                InstallUserService();

                NotifyUser("Coding Time Tracker installed and running in background.");

                return 0;
            }


            if (!DetectSteam())
            {
                NotifyUser("Steam installation not detected.");
                return 1;
            }

            if (!IsGameInstalled())
            {
                NotifyUser("Kode Studio is not installed. Redirecting to store.");
                StartProcessDetached("xdg-open", StoreUrl);
                return 1;
            }

            EnsureStubExists();

            if (!IsProcessRunning(SteamProcessName))
                StartSteam();

            await WaitForSteamReadyAsync(WaitForSteamTimeoutMs, _cts.Token);

            await RunEditorWatcherAsync(_cts.Token);

            return 0;
        }

        private static bool DetectSteam()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string[] possibleRoots =
            {
                Path.Combine(home, ".var/app/com.valvesoftware.Steam/.local/share/Steam"),  // Flatpak
                Path.Combine(home, ".local/share/Steam"),                                   // Native
                Path.Combine(home, ".steam/steam"),                                         // Old
                Path.Combine(home, "Steam"),                                                // Custom ~/Steam
                Path.Combine(home, "snap/steam/current/.local/share/Steam")                 // Snap
            };

            foreach (var root in possibleRoots)
            {
                if (Directory.Exists(root))
                {
                    SteamRoot = root;
                    SteamApps = Path.Combine(root, "steamapps");

                    GameDir = Path.Combine(SteamApps, "common/Kode Studio/Linux");

                    StubPath = Path.Combine(GameDir, GameProcessName);

                    return true;
                }
            }

            return false;
        }

        private static void RunSystemCtl(string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = "--user " + args,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static bool IsGameInstalled()
        {
            string manifest = Path.Combine(SteamApps, "appmanifest_779260.acf");

            return File.Exists(manifest) && Directory.Exists(GameDir);
        }

        private static void EnsureStubExists()
        {
            try
            {
                Directory.CreateDirectory(GameDir);

                string backup = StubPath + ".original";

                if (File.Exists(StubPath) && !File.Exists(backup))
                {
                    Console.WriteLine("Backing up original kodestudio...");
                    File.Move(StubPath, backup);
                }

                Console.WriteLine("Deploying kodestudio stub...");

                var assembly = Assembly.GetExecutingAssembly();

                using var stream =
                    assembly.GetManifestResourceStream("CodingTimeTrackerForSteam.Resources.kodestudio");

                if (stream == null)
                    throw new Exception("Embedded stub not found.");

                using var file = File.Create(StubPath);
                stream.CopyTo(file);

                Process.Start("chmod", $"+x \"{StubPath}\"")?.WaitForExit();

                Console.WriteLine("Stub deployed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stub deployment failed: {ex.Message}");
            }
        }

        private static void StartSteam()
        {
            try
            {
                Console.WriteLine("Starting Steam...");
                StartProcessDetached("xdg-open", "steam://open/main");
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Steam start failed: {ex.Message}");
            }
        }

        private static void StartGame()
        {
            try
            {
                Console.WriteLine("Launching game via Steam URI...");
                StartProcessDetached("xdg-open", SteamUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Game start failed: {ex.Message}");
            }
        }

        private static async Task WaitForSteamReadyAsync(int timeoutMs, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (IsProcessRunning(SteamProcessName) &&
                    IsProcessRunning(SteamWebHelperProcessName))
                {
                    Console.WriteLine("Steam ready.");
                    return;
                }

                await Task.Delay(1000, ct);
            }

            Console.WriteLine("Steam readiness timeout.");
        }

        private static async Task RunEditorWatcherAsync(CancellationToken ct)
        {
            Console.WriteLine("Editor watcher started.");

            while (!ct.IsCancellationRequested)
            {
                bool editorRunning = IsAnyCodeEditorRunning();

                if (!editorRunning)
                {
                    KillGameProcess();
                    _launchRequested = false;
                }
                else
                {
                    if (!IsGameRunning() && !_launchRequested)
                    {
                        if ((DateTime.Now - _lastLaunch).TotalSeconds > 15)
                        {
                            Console.WriteLine("Editor detected -> starting kodestudio.");

                            StartGame();

                            _launchRequested = true;
                            _lastLaunch = DateTime.Now;
                        }
                    }
                }

                if (IsGameRunning())
                    _launchRequested = false;

                await Task.Delay(EditorCheckIntervalMs, ct);
            }
        }

        private static bool IsAnyCodeEditorRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p =>
                {
                    var name = p.ProcessName?.ToLowerInvariant();
                    return name != null && CodeEditors.Contains(name);
                });
            }
            catch
            {
                return false;
            }
        }

        private static bool IsGameRunning()
        {
            return IsProcessRunning(GameProcessName);
        }

        private static bool IsProcessRunning(string name)
        {
            try
            {
                return Process.GetProcessesByName(name).Any();
            }
            catch
            {
                return false;
            }
        }

        private static void KillGameProcess()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(GameProcessName))
                {
                    try { p.Kill(true); } catch { }
                }
            }
            catch { }
        }

        private static void NotifyUser(string message)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notify-send",
                    Arguments = $"\"Coding Time Tracker\" \"{message}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                Console.WriteLine(message);
            }
        }

        private static void InstallBinary()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string binDir = Path.Combine(home, ".local/bin");
            Directory.CreateDirectory(binDir);

            string target = Path.Combine(binDir, "CodingTimeTrackerForSteam");

            string current = Environment.ProcessPath;

            if (string.IsNullOrEmpty(current))
                return;

            if (current != target)
            {
                File.Copy(current, target, true);
                Process.Start("chmod", $"+x \"{target}\"")?.WaitForExit();
            }
        }

        private static void InstallUserService()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string systemdDir = Path.Combine(home, ".config/systemd/user");
            Directory.CreateDirectory(systemdDir);

            string servicePath = Path.Combine(systemdDir, "codingtimetracker.service");

            string service = """
        [Unit]
        Description=Coding Time Tracker for Steam
        After=graphical-session.target

        [Service]
        Type=simple
        ExecStart=%h/.local/bin/CodingTimeTrackerForSteam
        Restart=always
        RestartSec=5

        [Install]
        WantedBy=graphical-session.target
        """;

            if (!File.Exists(servicePath))
                File.WriteAllText(servicePath, service);

            RunSystemCtl("daemon-reload");
            Thread.Sleep(300);

            RunSystemCtl("enable codingtimetracker.service");
            Thread.Sleep(300);

            RunSystemCtl("start codingtimetracker.service");
        }

        private static void StartProcessDetached(string file, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process start error: {ex.Message}");
            }
        }
    }
}