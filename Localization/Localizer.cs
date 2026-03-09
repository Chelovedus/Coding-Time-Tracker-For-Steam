using System.Globalization;

namespace CodingTimeTrackerForSteam.Localization;

internal record LocaleData(string InstallMessage, string InstallTitle, string[] MenuItems);

internal static class Localizer
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodingTimeTrackerForSteam");

    private static readonly string LangFile = Path.Combine(SettingsDir, "language.txt");

    public static readonly (string Code, string NativeName)[] SupportedLanguages =
    [
        ("ar", "العربية"),
        ("cs", "Čeština"),
        ("da", "Dansk"),
        ("de", "Deutsch"),
        ("en", "English"),
        ("es", "Español"),
        ("fi", "Suomi"),
        ("fr", "Français"),
        ("hu", "Magyar"),
        ("it", "Italiano"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("nl", "Nederlands"),
        ("no", "Norsk"),
        ("pt", "Português"),
        ("ru", "Русский"),
        ("sv", "Svenska"),
        ("tr", "Türkçe"),
        ("zh-Hans", "简体中文"),
        ("zh-Hant", "繁體中文"),
    ];

    public static string? SavedLanguageCode { get; private set; }

    public static LocaleData Get()
    {
        var saved = LoadSavedLanguage();
        CultureInfo culture;

        if (saved != null)
        {
            culture = new CultureInfo(saved);
            SavedLanguageCode = saved;
        }
        else
        {
            culture = CultureInfo.CurrentUICulture;
            SavedLanguageCode = null;

            if (culture.TwoLetterISOLanguageName == "zh")
            {
                string name = culture.Name;
                culture = (name.Contains("TW") || name.Contains("HK") || name.Contains("MO"))
                    ? new CultureInfo("zh-Hant")
                    : new CultureInfo("zh-Hans");
            }
        }

        Strings.Culture = culture;

        return new LocaleData(
            Strings.InstallMessage,
            Strings.InstallTitle,
            [Strings.MenuItem_GitHub, Strings.MenuItem_Developer, Strings.MenuItem_Exit]);
    }

    public static void SetLanguage(string? cultureCode)
    {
        SavedLanguageCode = cultureCode;
        SaveLanguage(cultureCode);
        Get();
    }

    private static string? LoadSavedLanguage()
    {
        try
        {
            if (File.Exists(LangFile))
            {
                var code = File.ReadAllText(LangFile).Trim();
                return string.IsNullOrEmpty(code) ? null : code;
            }
        }
        catch { }
        return null;
    }

    private static void SaveLanguage(string? cultureCode)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(LangFile, cultureCode ?? "");
        }
        catch { }
    }
}
