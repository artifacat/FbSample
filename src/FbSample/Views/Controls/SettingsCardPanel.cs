namespace FbSample.Views.Controls;

/// <summary>Preserves a settings card's unlimited height when Windows Forms scales its maximum width.</summary>
public sealed class SettingsCardPanel : AntdUI.Panel
{
    /// <inheritdoc />
    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        int height = Height;
        base.ScaleControl(factor, specified);
        if (MaximumSize.Width > 0 && MaximumSize.Height == 0)
        {
            // Windows Forms reapplies the scaled MaximumSize and clamps its unrestricted height to zero.
            Height = (specified & BoundsSpecified.Height) != 0 ? (int)Math.Round(height * factor.Height) : height;
        }
    }
}
