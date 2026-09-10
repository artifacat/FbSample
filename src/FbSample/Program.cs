using System.Drawing.Text;

using AntdUI;

using FbSample.Localization;
using FbSample.Views;

namespace FbSample;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationConfiguration.Initialize();
        AppLocalization.Initialize();
        Config.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        Config.TextRenderingHighQuality = true;
        Config.ShowInWindow = true;
        Config.Theme().Dark("#000", "#fff").Light("#fff", "#000").FormBorderColor();
        using var mainForm = new MainForm();
        Application.Run(mainForm);
    }
}
