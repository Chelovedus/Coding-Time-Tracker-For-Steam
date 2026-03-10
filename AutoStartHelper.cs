using System;
using System.Diagnostics;
using System.IO;

namespace CodingTimeTrackerForSteam.Linux
{
    internal static class AutoStartHelper
    {
        private const string AppName = "CodingTimeTrackerForSteam";
        private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string LocalBinDir = Path.Combine(HomeDir, ".local", "bin");
        private static readonly string AutostartDir = Path.Combine(HomeDir, ".config", "autostart");
        private static readonly string TargetExecutable = Path.Combine(LocalBinDir, AppName);

        public static void SetupAutoStart(string sourceExePath)
        {
            try
            {
                Directory.CreateDirectory(LocalBinDir);

                File.Copy(sourceExePath, TargetExecutable, overwrite: true);
                Process.Start("chmod", $"+x \"{TargetExecutable}\"")?.WaitForExit();
                Console.WriteLine($"Copied program to {TargetExecutable}");

                Directory.CreateDirectory(AutostartDir);

                string desktopFile = Path.Combine(AutostartDir, $"{AppName}.desktop");
                string desktopContent = $@"
[Desktop Entry]
Type=Application
Name={AppName}
Exec={TargetExecutable}
X-GNOME-Autostart-enabled=true
NoDisplay=false
";

                File.WriteAllText(desktopFile, desktopContent);
                Console.WriteLine($"Autostart entry created: {desktopFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to setup autostart: {ex.Message}");
            }
        }
    }
}