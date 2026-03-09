#nullable enable
using System.Globalization;
using System.Resources;

namespace CodingTimeTrackerForSteam.Localization;

internal static class Strings
{
    private static ResourceManager? _resourceManager;

    internal static ResourceManager ResourceManager =>
        _resourceManager ??= new ResourceManager(
            "CodingTimeTrackerForSteam.Localization.Strings",
            typeof(Strings).Assembly);

    internal static CultureInfo? Culture { get; set; }

    internal static string InstallMessage =>
        ResourceManager.GetString(nameof(InstallMessage), Culture)!;

    internal static string InstallTitle =>
        ResourceManager.GetString(nameof(InstallTitle), Culture)!;

    internal static string MenuItem_GitHub =>
        ResourceManager.GetString(nameof(MenuItem_GitHub), Culture)!;

    internal static string MenuItem_Developer =>
        ResourceManager.GetString(nameof(MenuItem_Developer), Culture)!;

    internal static string MenuItem_Exit =>
        ResourceManager.GetString(nameof(MenuItem_Exit), Culture)!;

    internal static string AlreadyRunningTitle =>
        ResourceManager.GetString(nameof(AlreadyRunningTitle), Culture)!;

    internal static string AlreadyRunningMessage =>
        ResourceManager.GetString(nameof(AlreadyRunningMessage), Culture)!;

    internal static string MenuItem_Language =>
        ResourceManager.GetString(nameof(MenuItem_Language), Culture)!;

    internal static string MenuItem_SystemDefault =>
        ResourceManager.GetString(nameof(MenuItem_SystemDefault), Culture)!;
}
