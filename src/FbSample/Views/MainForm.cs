using System.Globalization;

using AntdUI;

using FbSample.Localization;
using FbSample.Views.Pages;

using Microsoft.Win32;

namespace FbSample.Views;

/// <summary>The AntdUI application shell and its designer-editable navigation.</summary>
public partial class MainForm : Window
{
    private readonly Stack<string> _navigationHistory = new();
    private readonly AntdUI.Panel _navigationFlyoutPanel;
    private readonly Controls.NavigationMenu _navigationFlyoutMenu;
    private bool _synchronizingNavigation;
    private string _currentPageKey = "Dashboard";

    /// <summary>Creates the shell without starting runtime services in the designer.</summary>
    public MainForm()
    {
        InitializeComponent();
        _dashboardPage.Name = "Dashboard";
        // Its own designer preserves the drawing source's DPI baseline independently of the main canvas.
        var flyout = new Controls.NavigationFlyoutControl { Visible = false };
        _navigationFlyoutPanel = flyout.Surface;
        _navigationFlyoutMenu = flyout.Menu;
        Controls.Add(flyout);
        _navigationMenu.FontChanged += AlignNavigationIcons;
        _navigationMenu.DpiChangedAfterParent += AlignNavigationIcons;
        _backButton.FontChanged += AlignNavigationIcons;
        _collapseButton.FontChanged += AlignNavigationIcons;
        AlignNavigationIcons(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!DesignMode)
        {
            InitializeRuntime();
        }
    }

    internal void InitializeRuntime()
    {
        _navigationMenu.InitializeFlyout(_navigationFlyoutPanel, _navigationFlyoutMenu);
        ClientSizeChanged += DismissNavigationFlyout;
        DpiChanged += DismissNavigationFlyout;
        Deactivate += DismissNavigationFlyout;
        Theme().Button(_themeButton).Call(ApplyPalette);
        ApplySystemTheme();
        ApplyPalette(!Config.IsLight);
        ApplyLanguage();
        AlignNavigationIcons(this, EventArgs.Empty);
        SelectCurrentMenu();
        SystemEvents.UserPreferenceChanged += SystemPreferenceChanged;
    }

    internal void NavigateTo(string pageKey)
    {
        string previousPageKey = _currentPageKey;
        ShowPage(pageKey);
        if (!string.Equals(previousPageKey, pageKey, StringComparison.Ordinal))
        {
            _navigationHistory.Push(previousPageKey);
        }

        SetNavigationCollapsed(true);
    }

    private void ShowPage(string pageKey)
    {
        if (string.Equals(_currentPageKey, pageKey, StringComparison.Ordinal))
        {
            SelectCurrentMenu();
            return;
        }

        UserControl? content = null;
        foreach (UserControl page in _contentPanel.Controls)
        {
            if (string.Equals(page.Name, pageKey, StringComparison.Ordinal))
            {
                content = page;
                break;
            }
        }

        if (content is null)
        {
            content = pageKey switch
            {
                "Devices" => new DevicesPage(),
                "Monitor" => new MonitorPage(),
                "LogsPage1" => new LogsPage1Page(),
                "LogsPage2" => new LogsPage2Page(),
                "DebugPage1" => new DebugPage1Page(),
                "Settings" => new SettingsPage(),
                _ => throw new ArgumentOutOfRangeException(nameof(pageKey), pageKey, "Unknown navigation page.")
            };
            content.Name = pageKey;
            content.Dock = DockStyle.Fill;
            content.BackColor = _contentPanel.BackColor;
            content.Visible = false;
            _contentPanel.Controls.Add(content);
            if (content is SettingsPage settings)
            {
                settings.Initialize(this);
            }
        }

        foreach (Control page in _contentPanel.Controls)
        {
            page.Visible = page == content;
        }

        content.BringToFront();
        _currentPageKey = pageKey;
        SelectCurrentMenu();
    }

    internal void SetLanguage(string languageName)
    {
        AppLocalization.SetLanguage(languageName);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        _navigationMenu.CloseFlyout();
        _synchronizingNavigation = true;
        try
        {
            _languageDropdown.SelectedValue = CultureInfo.CurrentUICulture.Name switch
            {
                "zh-CN" => "简体中文",
                "zh-TW" => "繁體中文",
                _ => "English"
            };
            foreach (AntdUI.MenuItem item in _navigationMenu.Items)
            {
                // Names are stable route keys assigned to every item in the designer.
                item.Text = AppLocalization.GetText(item.Name!);
                foreach (AntdUI.MenuItem child in item.Sub)
                {
                    child.Text = AppLocalization.GetText(child.Name!);
                }
            }

            _searchInput.PlaceholderText = AppLocalization.GetText("Search");
            _searchInput.AccessibleName = AppLocalization.GetText("Search");
            _languageDropdown.AccessibleName = AppLocalization.GetText("Language");
            _themeButton.AccessibleName = AppLocalization.GetText("Theme");
            _aboutButton.AccessibleName = AppLocalization.GetText("About");
            _backButton.AccessibleName = AppLocalization.GetText("Back");
            _navigationTooltips.SetTip(_backButton, _backButton.AccessibleName);
            UpdateCollapseLabel();
            foreach (SettingsPage settings in _contentPanel.Controls.OfType<SettingsPage>())
            {
                settings.ApplyLanguage();
            }
        }
        finally
        {
            _synchronizingNavigation = false;
        }

        FilterMenu();
        SelectCurrentMenu();
        Refresh();
    }

    private void FilterMenu()
    {
        string filter = _searchInput.Text.Trim();
        foreach (AntdUI.MenuItem item in _navigationMenu.Items)
        {
            // Every item has display text in both the designer and the complete language resources.
            bool rootMatches = item.Text!.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || item.Name!.Contains(filter, StringComparison.OrdinalIgnoreCase);
            bool childMatches = false;
            foreach (AntdUI.MenuItem child in item.Sub)
            {
                child.Visible = rootMatches
                    || child.Text!.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                    || child.Name!.Contains(filter, StringComparison.OrdinalIgnoreCase);
                childMatches |= child.Visible;
            }

            item.Visible = rootMatches || childMatches;
            if (filter.Length > 0 && childMatches)
            {
                item.Expand = true;
            }
        }

        _navigationMenu.Refresh();
    }

    private void SelectCurrentMenu()
    {
        if (_synchronizingNavigation)
        {
            return;
        }

        _synchronizingNavigation = true;
        try
        {
            _navigationMenu.CurrentPageKey = _currentPageKey;
            var groups = _navigationMenu.Items.Where(static item => item.Sub.Count > 0)
                .Select(static item => (Item: item, Expanded: item.Expand)).ToArray();
            _navigationMenu.Select(_navigationMenu.FindName(_currentPageKey)
                ?? throw new InvalidOperationException("The selected page has no navigation item."), false);
            foreach (var group in groups)
            {
                group.Item.Expand = group.Expanded;
            }
        }
        finally
        {
            _synchronizingNavigation = false;
        }
    }

    private void NavigationClicked(object sender, MenuItemEventArgs e)
    {
        if (!_synchronizingNavigation && e.Item.Sub.Count == 0)
        {
            NavigateTo(e.Item.Name ?? throw new InvalidOperationException("The navigation item has no route."));
        }
    }

    private void SearchChanged(object? sender, EventArgs e)
    {
        _navigationMenu.CloseFlyout();
        FilterMenu();
    }

    private void CollapseClicked(object? sender, EventArgs e)
    {
        SetNavigationCollapsed(!_navigationMenu.Collapsed);
    }

    private void ShellLayout(object? sender, LayoutEventArgs e)
    {
        _navigationPanel.Height = ClientSize.Height;
    }

    private void DismissNavigationFlyout(object? sender, EventArgs e)
    {
        _navigationMenu.CloseFlyout();
    }

    private void AlignNavigationIcons(object? sender, EventArgs e)
    {
        _navigationPanel.PerformLayout();
        int collapsedWidth = (int)Math.Round(58 * (DeviceDpi / 96F)) - _navigationDivider.Width;
        _navigationMenu.AlignIcons(collapsedWidth);

        // Measure before handles exist as well, so the designer uses the same native Menu geometry.
        int fontHeight = _navigationMenu.GDI(canvas => canvas.MeasureString(Config.NullText, _navigationMenu.Font).Height);
        int rowHeight = fontHeight + 2 * (int)(_navigationMenu.Gap!.Value * _navigationMenu.Dpi);
        int spacing = (int)(_navigationMenu.itemMargin!.Value * _navigationMenu.Dpi);
        int padding = _navigationMenu.Padding.Right;
        int width = collapsedWidth - padding * 2;
        _navigationHeaderPanel.Height = padding + rowHeight + spacing;
        _navigationToolsPanel.Height = rowHeight + spacing;
        _backButton.Bounds = new Rectangle(padding, padding, width, rowHeight);
        _collapseButton.Bounds = new Rectangle(padding, 0, width, rowHeight);

        int iconSize = (int)(fontHeight * _navigationMenu.IconRatio);
        int iconRight = padding + (width - iconSize) / 2 + iconSize;
        int textPadding = (int)Math.Round(6 * (DeviceDpi / 96F));
        int textLeft = collapsedWidth + _navigationDivider.Width + textPadding;
        bool canGoBack = _navigationHistory.Count > 0;
        _backButton.Toggle = canGoBack;
        _backButton.Enabled = _backButton.Visible = canGoBack;
        _navigationLogo.Bounds = new Rectangle(canGoBack ? textLeft : iconRight - iconSize,
            padding + (rowHeight - iconSize) / 2, iconSize, iconSize);
        _navigationLogo.Visible = !canGoBack || !_navigationMenu.Collapsed;
        int brandLeft = canGoBack ? textLeft + iconSize + (int)Math.Round(8 * (DeviceDpi / 96F)) : textLeft;
        int brandRight = (int)Math.Round(250 * (DeviceDpi / 96F)) - _navigationDivider.Width - textPadding;
        _brandLabel.Bounds = new Rectangle(brandLeft, padding, brandRight - brandLeft, rowHeight);
        _brandLabel.Visible = !_navigationMenu.Collapsed;

        // Keep labels outside the rail; Menu expresses its icon gap in whole logical pixels.
        _navigationMenu.IconGap = (int)Math.Ceiling((textLeft - iconRight) / _navigationMenu.Dpi);
        foreach (AntdUI.Button button in new[] { _backButton, _collapseButton })
        {
            int buttonFontHeight = button.GDI(canvas => canvas.MeasureText(Config.NullText, button.Font).Height);
            // Button scales icon-only content by 1.125; target the middle of its integer pixel interval.
            button.IconRatio = (iconSize + 0.5F) / (buttonFontHeight * 1.125F);
        }
    }

    private void BackClicked(object? sender, EventArgs e)
    {
        ShowPage(_navigationHistory.Pop());
        SetNavigationCollapsed(true);
        if (_navigationHistory.Count == 0)
        {
            _collapseButton.Focus();
        }
    }

    private void SetNavigationCollapsed(bool collapsed)
    {
        _navigationMenu.CloseFlyout();
        _navigationMenu.Collapsed = collapsed;
        _navigationPanel.Width = (int)Math.Round((collapsed ? 58 : 250) * (DeviceDpi / 96F));
        AlignNavigationIcons(this, EventArgs.Empty);
        ApplyPalette(!Config.IsLight);
        UpdateCollapseLabel();
    }

    private void ApplyPalette(bool dark)
    {
        Color navigation = dark
            ? _navigationMenu.Collapsed ? Color.FromArgb(32, 37, 44) : Color.FromArgb(40, 40, 40)
            : _navigationMenu.Collapsed ? Color.FromArgb(239, 244, 249) : Color.FromArgb(247, 247, 247);
        Color content = dark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(243, 243, 243);
        Color foreground = dark ? Color.FromArgb(238, 238, 238) : Color.FromArgb(38, 38, 38);
        _navigationPanel.Back = _navigationPanel.BackColor = navigation;
        _contentPanel.Back = _contentPanel.BackColor = content;
        _workspacePanel.Back = _workspacePanel.BackColor = content;
        _navigationDivider.Back = dark ? Color.FromArgb(62, 65, 70) : Color.FromArgb(225, 230, 234);
        _navigationFlyoutPanel.Back = dark ? Color.FromArgb(40, 40, 40) : Color.FromArgb(247, 247, 247);
        _navigationFlyoutPanel.BackColor = content;
        _navigationFlyoutPanel.BorderColor = dark ? Color.FromArgb(62, 65, 70) : Color.FromArgb(204, 204, 204);
        _brandLabel.ForeColor = foreground;
        _titleBar.BackColor = dark ? Color.FromArgb(37, 37, 37) : Color.FromArgb(247, 247, 247);
        foreach (AntdUI.Button button in new[] { _backButton, _collapseButton })
        {
            button.ForeColor = button.ForeHover = button.ForeActive = foreground;
            button.BackHover = dark ? Color.FromArgb(51, 54, 59) : Color.FromArgb(239, 239, 239);
            button.BackActive = dark ? Color.FromArgb(65, 68, 73) : Color.FromArgb(226, 226, 226);
        }

        _navigationMenu.ApplyPalette(dark);
        foreach (UserControl page in _contentPanel.Controls)
        {
            page.BackColor = content;
            if (page is SettingsPage settings)
            {
                settings.ApplyPalette(dark);
            }
        }
    }

    private void UpdateCollapseLabel()
    {
        _collapseButton.AccessibleName = AppLocalization.GetText(
            _navigationMenu.Collapsed ? "ExpandNavigation" : "CollapseNavigation");
        _navigationTooltips.SetTip(_collapseButton, _collapseButton.AccessibleName);
    }

    private void LanguageChanged(object sender, ObjectNEventArgs e)
    {
        if (_synchronizingNavigation)
        {
            return;
        }

        SetLanguage(e.Value switch
        {
            "English" => "en-US",
            "简体中文" => "zh-CN",
            "繁體中文" => "zh-TW",
            _ => throw new InvalidOperationException("Unknown language selection.")
        });
    }

    private void ThemeClicked(object? sender, EventArgs e)
    {
        Config.IsLight = !Config.IsLight;
    }

    private void AboutClicked(object? sender, EventArgs e)
    {
        using var about = new AboutControl();
        about.ResetBackColor();
        AntdUI.Modal.open(new AntdUI.Modal.Config(this, AppLocalization.GetText("About"), about)
        {
            CloseIcon = true,
            BtnHeight = 0
        });
    }

    private void SystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && !IsDisposed && IsHandleCreated)
        {
            // SystemEvents may run outside the UI thread; marshal the actual Windows theme value.
            BeginInvoke(ApplySystemTheme);
        }
    }

    private static void ApplySystemTheme()
    {
        // Windows uses the light app theme when this optional personalization value is absent.
        object? value = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        Config.IsLight = value switch
        {
            null or 1 => true,
            0 => false,
            _ => throw new InvalidOperationException("Windows returned an invalid app theme value.")
        };
    }
}
