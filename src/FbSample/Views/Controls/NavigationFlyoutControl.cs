namespace FbSample.Views.Controls;

/// <summary>Provides the independently editable drawing controls and DPI baseline of navigation flyouts.</summary>
public partial class NavigationFlyoutControl : UserControl
{
    /// <summary>Creates the drawing source without opening a flyout window.</summary>
    public NavigationFlyoutControl()
    {
        InitializeComponent();
    }

    internal AntdUI.Panel Surface => _navigationFlyoutPanel;

    internal NavigationMenu Menu => _navigationFlyoutMenu;
}
