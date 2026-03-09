using System.Diagnostics;

namespace CodingTimeTrackerForSteam.Steam;

internal class SteamManager
{
    private const string SteamProcess     = "steam";
    private const string WebHelperProcess = "steamwebhelper";

    public bool IsRunning() =>
        Process.GetProcessesByName(SteamProcess).Any();

    public bool IsFullyReady() =>
        IsRunning() && Process.GetProcessesByName(WebHelperProcess).Any();

    public void Start()
    {
        try
        {
            Process.Start(new ProcessStartInfo("steam://") { UseShellExecute = true });
            Console.WriteLine("Steam launched. Waiting for initialization...");
            Thread.Sleep(5000);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start Steam: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void WaitUntilReady(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        int tick = 0;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (IsFullyReady())
            {
                Console.WriteLine("Steam is ready.");
                return;
            }

            Thread.Sleep(1000);

            if (++tick % 10 == 0)
                Console.WriteLine($"Waiting for Steam... ({sw.ElapsedMilliseconds / 1000}s)");
        }

        Console.WriteLine("Steam ready timeout reached, continuing anyway...");
    }
}
