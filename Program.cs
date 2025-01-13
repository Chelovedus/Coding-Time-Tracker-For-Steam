using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CodingTimeTrackerForSteam
{
    internal static class Program
    {
        private static NotifyIcon _trayIcon;
        private static Process _gameProcess;
        private static System.Threading.Timer _editorCheckTimer;
        private static int _errorCount = 0;
        private static bool _isReady = false;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SwHide = 0;
        private const int SwShow = 5;

        private static readonly (string message, string title)[] _localizations =
        {
            ("You need to install Kode Studio from the Steam Store.", "Please install Kode Studio."),
            ("Вам необходимо установить Kode Studio из магазина Steam.", "Пожалуйста, установите Kode Studio."),
            ("Necesitas instalar Kode Studio desde la tienda de Steam.", "Por favor, instala Kode Studio."),
            ("Sie müssen Kode Studio aus dem Steam Store installieren.", "Bitte installieren Sie Kode Studio."),
            ("Vous devez installer Kode Studio depuis la boutique Steam.", "Veuillez installer Kode Studio."),
            ("Devi installare Kode Studio dal negozio Steam.", "Per favore, installa Kode Studio."),
            ("Você precisa instalar o Kode Studio na Loja Steam.", "Por favor, instale o Kode Studio."),
            ("게임 스토어에서 Kode Studio를 설치해야 합니다.", "Kode Studio를 설치하십시오."),
            ("您需要从 Steam 商店安装 Kode Studio。", "请安装 Kode Studio。"),
            ("您需要從 Steam 商店安裝 Kode Studio。", "請安裝 Kode Studio。"),
            ("Kode StudioはSteamストアからインストールする必要があります。", "Kode Studioをインストールしてください。"),
            ("Je moet Kode Studio vanuit de Steam-winkel installeren.", "Installeer Kode Studio alstublieft."),
            ("Du behöver installera Kode Studio från Steam-butiken.", "Vänligen installera Kode Studio."),
            ("Du skal installere Kode Studio fra Steam-butikken.", "Installer venligst Kode Studio."),
            ("Du må installere Kode Studio fra Steam-butikken.", "Venligst installer Kode Studio."),
            ("Sinun on asennettava Kode Studio Steam-kaupasta.", "Asenna Kode Studio."),
            ("Kode Studio'yu Steam Mağazasından yüklemeniz gerekiyor.", "Lütfen Kode Studio'yu yükleyin."),
            ("Você precisa instalar o Kode Studio na loja Steam.", "Por favor, instale o Kode Studio."),
            ("Musíte nainstalovat Kode Studio z obchodu Steam.", "Prosím, nainstalujte Kode Studio."),
            ("A Kode Studio-t a Steam boltjából kell telepíteni.", "Kérjük, telepítse a Kode Studio-t.")
        };

        private static readonly Dictionary<string, string[]> _localize = new()
        {
            { "en", new[] { "GitHub", "Developer Page", "Exit" } },
            { "ru", new[] { "GitHub", "Страница разработчика", "Выход" } },
            { "es", new[] { "GitHub", "Página del desarrollador", "Salir" } },
            { "de", new[] { "GitHub", "Entwicklerseite", "Beenden" } },
            { "fr", new[] { "GitHub", "Page du développeur", "Sortir" } },
            { "it", new[] { "GitHub", "Pagina dello sviluppatore", "Uscita" } },
            { "pt", new[] { "GitHub", "Página do desenvolvedor", "Sair" } },
            { "ko", new[] { "깃허브", "개발자 페이지", "종료" } },
            { "zh-Hans", new[] { "GitHub", "开发者页面", "退出" } },
            { "zh-Hant", new[] { "GitHub", "開發者頁面", "退出" } },
            { "ja", new[] { "GitHub", "開発者ページ", "終了" } },
            { "nl", new[] { "GitHub", "Ontwikkelaarspagina", "Afsluiten" } },
            { "sv", new[] { "GitHub", "Utvecklarens sida", "Avsluta" } },
            { "da", new[] { "GitHub", "Udviklerens side", "Afslut" } },
            { "no", new[] { "GitHub", "Utviklerside", "Avslutt" } },
            { "fi", new[] { "GitHub", "Kehittäjän sivu", "Poistu" } },
            { "tr", new[] { "GitHub", "Geliştirici Sayfası", "Çıkış" } },
            { "ar", new[] { "GitHub", "صفحة المطور", "خروج" } },
            { "cs", new[] { "GitHub", "Stránka vývojáře", "Odhlásit se" } },
            { "hu", new[] { "GitHub", "Fejlesztői oldal", "Kilépés" } }
        };

        private static readonly string[] CodeEditors =
        {
            "code", "devenv", "idea64", "pycharm64", "rider64", "clion64",
            "phpstorm64", "webstorm64", "studio64", "eclipse", "netbeans64",
            "codeblocks", "qtcreator", "kdevelop", "jdev", "monodevelop",
            "arduino", "sublime_text", "atom", "notepad++", "brackets",
            "geany", "kate", "gedit", "komodo", "jedit", "bbedit",
            "spyder", "thonny", "rstudio", "vim", "nvim", "emacs"
        };

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CodingTimeTrackerForSteam.Resources.vscode.ico"))
            {
                if (iconStream == null)
                {
                    MessageBox.Show("Icon not found in resources", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _trayIcon = new NotifyIcon
                {
                    Icon = new Icon(iconStream),
                    Visible = true,
                    Text = "Coding Time Tracker For Steam"
                };

                var contextMenu = new ContextMenuStrip();
                string userLanguage = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;

                if (!_localize.ContainsKey(userLanguage))
                {
                    userLanguage = "en";
                }

                contextMenu.Items.Add(_localize[userLanguage][0], null, OnGitHubMenuItemClick);
                contextMenu.Items.Add(_localize[userLanguage][1], null, OnDeveloperPageMenuItemClick);
                contextMenu.Items.Add(_localize[userLanguage][2], null, OnExitMenuItemClick);

                _trayIcon.ContextMenuStrip = contextMenu;
                _gameProcess = Process.Start(new ProcessStartInfo("steam://rungameid/779260") { UseShellExecute = true });
                CheckProgram();
                _editorCheckTimer = new System.Threading.Timer(CheckEditorsAndManageGame, null, 0, 1000);
                Application.Run();
            }
        }

        private static bool IsAnyCodeEditorRunning() =>
            Process.GetProcesses().Any(p => CodeEditors.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase));

        private static void CheckProgram()
        {
            var timer = new System.Timers.Timer(7000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        private static void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            if (!_isReady)
            {
                string[] supportedLanguages = { "en", "ru", "es", "de", "fr", "it", "pt", "ko", "zh-Hans", "zh-Hant", 
                                                "ja", "nl", "sv", "da", "no", "fi", "tr", "ar", "cs", "hu" };

                var cultureInfo = Thread.CurrentThread.CurrentCulture;
                string languageCode = cultureInfo.TwoLetterISOLanguageName;
                int index = Array.IndexOf(supportedLanguages, languageCode);

                if (index == -1)
                {
                    index = 0;
                }

                var (message, title) = _localizations[index];
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Process.Start(new ProcessStartInfo("https://store.steampowered.com/app/779260") { UseShellExecute = true });
                Application.Exit();
            }
        }

        private static void HideGameWindow()
        {
            const string targetWindowTitle = "Kode Studio";
            EnumWindows((hWnd, lParam) =>
            {
                int length = GetWindowTextLength(hWnd);
                if (length > 0)
                {
                    var windowTitle = new StringBuilder(length + 1);
                    GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                    if (windowTitle.ToString().Equals(targetWindowTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        ShowWindow(hWnd, SwHide);
                        Console.WriteLine($"Game window \"{windowTitle}\" is hidden.");
                        _isReady = true;
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        private static void CheckEditorsAndManageGame(object state)
        {
            HideGameWindow();
            if (_isReady)
            {
                if (IsAnyCodeEditorRunning())
                {
                    if (_gameProcess == null || _gameProcess.HasExited)
                    {
                        try
                        {
                            if (!IsGameRunning())
                            {
                                _gameProcess = Process.Start(new ProcessStartInfo("steam://rungameid/779260") { UseShellExecute = true });
                                Console.WriteLine("Game launched.");
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else if (_gameProcess != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("taskkill", "/F /IM \"Kode Studio.exe\"") { UseShellExecute = true });
                    Console.WriteLine("Game closed.");
                    _gameProcess = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing the game: {ex.Message}");
                }
            }
        }

        private static bool IsGameRunning() =>
            Process.GetProcesses().Any(p => p.ProcessName.Equals("Kode Studio", StringComparison.OrdinalIgnoreCase));

        private static void OnGitHubMenuItemClick(object sender, EventArgs e) =>
            Process.Start(new ProcessStartInfo("https://github.com/Chelovedus/Coding-Time-Tracker-For-Steam") { UseShellExecute = true });

        private static void OnDeveloperPageMenuItemClick(object sender, EventArgs e) =>
            Process.Start(new ProcessStartInfo("https://steamcommunity.com/id/superfrost/") { UseShellExecute = true });

        private static void OnExitMenuItemClick(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Process.Start(new ProcessStartInfo("taskkill", "/F /IM \"Kode Studio.exe\"") { UseShellExecute = true });
            Application.Exit();
        }
    }
}
