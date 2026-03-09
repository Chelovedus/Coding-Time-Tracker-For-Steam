using System.Diagnostics;
using System.Reflection;
using CodingTimeTrackerForSteam.Localization;

namespace CodingTimeTrackerForSteam.UI;

internal class TrayIconManager : IDisposable
{
    private const string IconResource  = "CodingTimeTrackerForSteam.Resources.vscode.ico";
    private const string GitHubUrl    = "https://github.com/Chelovedus/Coding-Time-Tracker-For-Steam";
    private const string DeveloperUrl = "https://steamcommunity.com/id/superfrost/";

    private readonly NotifyIcon _trayIcon;

    public event Action? ExitRequested;

    public TrayIconManager()
    {
        using var iconStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(IconResource);

        if (iconStream is null)
        {
            MessageBox.Show("Icon not found in resources.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
            _trayIcon = new NotifyIcon();
            return;
        }

        _trayIcon = new NotifyIcon
        {
            Icon    = new Icon(iconStream),
            Visible = true,
            Text    = "Coding Time Tracker For Steam"
        };

        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var locale = Localizer.Get();

        var menu = new ContextMenuStrip();
        menu.Items.Add(locale.MenuItems[0], null, (_, _) => OpenUrl(GitHubUrl));
        menu.Items.Add(locale.MenuItems[1], null, (_, _) => OpenUrl(DeveloperUrl));
        menu.Items.Add(new ToolStripSeparator());

        var langMenu = new ToolStripMenuItem(Strings.MenuItem_Language);

        var autoItem = new ToolStripMenuItem(Strings.MenuItem_SystemDefault)
        {
            Checked = Localizer.SavedLanguageCode == null
        };
        autoItem.Click += (_, _) => { Localizer.SetLanguage(null); RebuildMenu(); };
        langMenu.DropDownItems.Add(autoItem);
        langMenu.DropDownItems.Add(new ToolStripSeparator());

        foreach (var (code, name) in Localizer.SupportedLanguages)
        {
            var item = new ToolStripMenuItem(name)
            {
                Checked = string.Equals(Localizer.SavedLanguageCode, code,
                    StringComparison.OrdinalIgnoreCase)
            };
            item.Click += (_, _) => { Localizer.SetLanguage(code); RebuildMenu(); };
            langMenu.DropDownItems.Add(item);
        }

        menu.Items.Add(langMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(locale.MenuItems[2], null, (_, _) => ExitRequested?.Invoke());

        var oldMenu = _trayIcon.ContextMenuStrip;
        _trayIcon.ContextMenuStrip = menu;
        oldMenu?.Dispose();
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    public void Dispose()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
