using System.Windows.Forms;
using CodingTimeTrackerForSteam.Core;
using CodingTimeTrackerForSteam.Localization;

namespace CodingTimeTrackerForSteam;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "CodingTimeTrackerForSteam", out bool createdNew);
        if (!createdNew)
        {
            Localizer.Get();
            MessageBox.Show(
                Strings.AlreadyRunningMessage,
                Strings.AlreadyRunningTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var controller = new AppController();
        controller.Start();

        Application.Run();
    }
}
