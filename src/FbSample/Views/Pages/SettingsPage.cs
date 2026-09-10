using System.Globalization;
using System.Text;

using AntdUI;

using FbSample.Localization;

namespace FbSample.Views.Pages;

/// <summary>
/// Provides the designer-editable AntdUI appearance and message settings.
/// </summary>
public partial class SettingsPage : UserControl
{
    private bool _arrangingSettings;

    /// <summary>
    /// Creates the settings controls without changing runtime configuration.
    /// </summary>
    public SettingsPage()
    {
        InitializeComponent();
        _basicSettingsPanel.SizeChanged += ArrangeSettings;
        _messageSettingsPanel.SizeChanged += ArrangeSettings;
        FontChanged += ArrangeSettings;
        DpiChangedAfterParent += ArrangeSettings;
        ArrangeSettings(this, EventArgs.Empty);
    }

    internal void Initialize(AntdUI.Window owner)
    {
        _animationSwitch.Checked = AntdUI.Config.Animation;
        _shadowSwitch.Checked = AntdUI.Config.ShadowEnabled;
        _scrollbarSwitch.Checked = AntdUI.Config.ScrollBarHide;
        _showInWindowSwitch.Checked = AntdUI.Config.ShowInWindow;
        _windowOffsetInput.Value = AntdUI.Config.NoticeWindowOffsetXY;
        ApplyLanguage();
        ApplyPalette(!AntdUI.Config.IsLight);

        _animationSwitch.CheckedChanged += static (_, e) =>
        {
            AntdUI.Config.Animation = e.Value;
        };
        _shadowSwitch.CheckedChanged += static (_, e) =>
        {
            AntdUI.Config.ShadowEnabled = e.Value;
        };
        _scrollbarSwitch.CheckedChanged += static (_, e) =>
        {
            AntdUI.Config.ScrollBarHide = e.Value;
        };
        _windowOffsetInput.ValueChanged += (_, e) =>
        {
            _windowOffsetInput.Status = e.Value is < int.MinValue or > int.MaxValue ? TType.Error : TType.None;
            if (_windowOffsetInput.Status == TType.None)
            {
                AntdUI.Config.NoticeWindowOffsetXY = (int)e.Value;
            }

            UpdateWindowOffsetDescription(!AntdUI.Config.IsLight);
            ArrangeSettings(this, EventArgs.Empty);
        };
        _showInWindowSwitch.CheckedChanged += (_, e) =>
        {
            AntdUI.Config.ShowInWindow = e.Value;
            string text = AppLocalization.GetText("SwitchSuccess");
            AntdUI.Message.success(owner, text, autoClose: 1);
            AntdUI.Notification.success(owner, AppLocalization.GetText("Tip"), text, autoClose: 1);
        };
    }

    internal void ApplyLanguage()
    {
        foreach (AntdUI.Label label in new[]
        {
            _settingsTitleLabel, _basicSettingsLabel, _messageSettingsLabel, _animationLabel, _shadowLabel, _scrollbarLabel,
            _showInWindowLabel, _windowOffsetLabel, _showInWindowDescription
        })
        {
            label.Text = AppLocalization.GetText(label.LocalizationText!);
        }

        _animationSwitch.AccessibleName = _animationLabel.Text;
        _shadowSwitch.AccessibleName = _shadowLabel.Text;
        _scrollbarSwitch.AccessibleName = _scrollbarLabel.Text;
        _showInWindowSwitch.AccessibleName = _showInWindowLabel.Text;
        _windowOffsetInput.AccessibleName = _windowOffsetLabel.Text;
        _showInWindowSwitch.AccessibleDescription = _showInWindowDescription.Text;
        UpdateWindowOffsetDescription(!AntdUI.Config.IsLight);
        ArrangeSettings(this, EventArgs.Empty);
    }

    internal void ApplyPalette(bool dark)
    {
        BackColor = dark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(243, 243, 243);
        _settingsStack.Back = _settingsStack.BackColor = BackColor;
        Color foreground = dark ? Color.FromArgb(238, 238, 238) : Color.FromArgb(38, 38, 38);
        foreach (AntdUI.Panel panel in new[] { _basicSettingsPanel, _messageSettingsPanel })
        {
            panel.BackColor = BackColor;
            panel.Back = dark ? Color.FromArgb(40, 40, 40) : Color.White;
            panel.BorderColor = dark ? Color.FromArgb(62, 65, 70) : Color.FromArgb(225, 225, 225);
        }

        foreach (AntdUI.Label label in new[]
        {
            _settingsTitleLabel, _basicSettingsLabel, _messageSettingsLabel, _animationLabel, _shadowLabel, _scrollbarLabel,
            _showInWindowLabel, _windowOffsetLabel
        })
        {
            label.ForeColor = foreground;
        }

        _showInWindowDescription.ForeColor =
            dark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(112, 112, 112);
        foreach (AntdUI.Divider divider in new[] { _animationDivider, _shadowDivider, _messageDivider })
        {
            divider.ForeColor = dark ? Color.FromArgb(62, 65, 70) : Color.FromArgb(225, 225, 225);
        }

        UpdateWindowOffsetDescription(dark);
    }

    private void UpdateWindowOffsetDescription(bool dark)
    {
        bool invalid = _windowOffsetInput.Status == TType.Error;
        // The native localization getter would otherwise replace this formatted explanation.
        _windowOffsetDescription.LocalizationText = null;
        _windowOffsetDescription.Text = invalid
            ? string.Format(CultureInfo.CurrentCulture,
                CompositeFormat.Parse(AppLocalization.GetText("WindowOffsetOutOfRange")),
                _windowOffsetInput.Value, AntdUI.Config.NoticeWindowOffsetXY)
            : AppLocalization.GetText("WindowOffsetDescription");
        _windowOffsetDescription.ForeColor = invalid
            ? dark ? Color.FromArgb(255, 120, 117) : Color.FromArgb(207, 19, 34)
            : dark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(112, 112, 112);
        _windowOffsetInput.AccessibleDescription = _windowOffsetDescription.Text;
    }

    private void ArrangeSettings(object? sender, EventArgs e)
    {
        if (_arrangingSettings)
        {
            return;
        }

        _arrangingSettings = true;
        try
        {
            int maximumWidth = (int)Math.Round(760 * (DeviceDpi / 96F));
            foreach (Control control in new Control[]
            {
                _settingsTitleLabel, _basicSettingsLabel, _basicSettingsPanel, _messageSettingsLabel, _messageSettingsPanel
            })
            {
                control.MaximumSize = new Size(maximumWidth, 0);
            }

            // A scrollbar can reduce the available width after the first height calculation.
            for (int pass = 0; pass < 2; pass++)
            {
                ArrangeRow(_animationRow, _animationLabel, _animationSwitch);
                ArrangeRow(_shadowRow, _shadowLabel, _shadowSwitch);
                ArrangeRow(_scrollbarRow, _scrollbarLabel, _scrollbarSwitch);
                ArrangeRow(_showInWindowRow, _showInWindowLabel, _showInWindowSwitch, _showInWindowDescription);
                ArrangeRow(_windowOffsetRow, _windowOffsetLabel, _windowOffsetInput, _windowOffsetDescription);
                _basicSettingsPanel.Height = _basicSettingsPanel.Height - _basicSettingsPanel.DisplayRectangle.Height + _animationRow.Height +
                    _animationDivider.Height + _shadowRow.Height + _shadowDivider.Height + _scrollbarRow.Height;
                _messageSettingsPanel.Height = _messageSettingsPanel.Height - _messageSettingsPanel.DisplayRectangle.Height + _showInWindowRow.Height +
                    _messageDivider.Height + _windowOffsetRow.Height;
                _settingsStack.PerformLayout();
            }
        }
        finally
        {
            _arrangingSettings = false;
        }
    }

    private void ArrangeRow(AntdUI.Panel row, AntdUI.Label title, Control input, AntdUI.Label? description = null)
    {
        float dpi = DeviceDpi / 96F;
        int padding = (int)Math.Round(18 * dpi);
        int gap = (int)Math.Round(4 * dpi);
        int inputWidth = (int)Math.Round(60 * dpi);
        int inputHeight = (int)Math.Round(26 * dpi);
        int textWidth = Math.Max(1, row.ClientSize.Width - padding * 2 - inputWidth - (int)Math.Round(16 * dpi));
        int titleHeight = title.GDI(canvas => canvas.MeasureText(title.Text, title.Font, textWidth).Height);
        int textHeight = titleHeight;
        int descriptionHeight = 0;
        if (description != null)
        {
            descriptionHeight = description.GDI(canvas => canvas.MeasureText(description.Text, description.Font, textWidth).Height);
            textHeight += gap + descriptionHeight;
        }

        row.Height = Math.Max((int)Math.Round((description == null ? 44 : 60) * dpi),
            textHeight + (int)Math.Round(16 * dpi));
        int textTop = (row.Height - textHeight) / 2;
        title.Bounds = new Rectangle(padding, textTop, textWidth, titleHeight);
        if (description != null)
        {
            description.Bounds = new Rectangle(padding, textTop + titleHeight + gap, textWidth, descriptionHeight);
        }

        input.Bounds = new Rectangle(row.ClientSize.Width - padding - inputWidth,
            (row.Height - inputHeight) / 2, inputWidth, inputHeight);
    }

    private void SettingControlEntered(object? sender, EventArgs e)
    {
        if (_settingsStack.YScroll is { Visible: true } scroll)
        {
            var input = (Control)sender!;
            Control row = input.Parent!;
            Control card = row.Parent!;
            Rectangle bounds = input.Bounds;
            bounds.Offset(row.Left + card.Left, row.Top + card.Top);
            scroll.SetValue(card.Parent!.ClientRectangle, bounds);
        }
    }
}
