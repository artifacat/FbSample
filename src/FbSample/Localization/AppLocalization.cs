using System.Globalization;
using System.Resources;

namespace FbSample.Localization;

internal sealed class AppLocalization : AntdUI.ILocalization
{
    private static readonly ResourceManager s_resources = new(
        "FbSample.Localization.Strings",
        typeof(AppLocalization).Assembly);

    public string? GetLocalizedString(string key) => s_resources.GetString(key, CultureInfo.CurrentUICulture);

    internal static void Initialize()
    {
        AntdUI.Localization.Provider = new AppLocalization();
        string cultureName = CultureInfo.CurrentUICulture.Name;
        string languageName = "en-US";

        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.Ordinal))
        {
            languageName = cultureName.Contains("-Hant", StringComparison.OrdinalIgnoreCase)
                || cultureName.EndsWith("-TW", StringComparison.OrdinalIgnoreCase)
                || cultureName.EndsWith("-HK", StringComparison.OrdinalIgnoreCase)
                || cultureName.EndsWith("-MO", StringComparison.OrdinalIgnoreCase)
                ? "zh-TW"
                : "zh-CN";
        }

        SetLanguage(languageName);
    }

    internal static string GetText(string key) => s_resources.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"The localization resource '{key}' is missing.");

    internal static void SetLanguage(string languageName)
    {
        if (languageName is not ("en-US" or "zh-CN" or "zh-TW"))
        {
            throw new ArgumentOutOfRangeException(nameof(languageName), languageName, "Unsupported application language.");
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(languageName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        AntdUI.Localization.SetLanguage(languageName);
    }
}
