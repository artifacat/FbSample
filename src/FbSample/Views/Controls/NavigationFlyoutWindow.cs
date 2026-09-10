using System.Runtime.ExceptionServices;

using AntdUI;

namespace FbSample.Views.Controls;

/// <summary>Composites the designer-owned navigation controls into a nonactivating alpha window.</summary>
internal sealed class NavigationFlyoutWindow : ILayeredForm
{
    private readonly AntdUI.Panel _panel;
    private readonly NavigationMenu _menu;
    private readonly SynchronizationContext _uiContext;
    private bool _panelDrawCompleted;
    private volatile bool _rendering;
    private volatile bool _closed;
    private int _framePending;
    private ExceptionDispatchInfo? _callbackFailure;

    internal NavigationFlyoutWindow(Form owner, AntdUI.Panel panel, NavigationMenu menu)
    {
        _panel = panel;
        _menu = menu;
        // WinForms installs the UI context when the designer-owned source handles are created.
        _uiContext = SynchronizationContext.Current!;
        PARENT = owner;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        alpha = 255;
        SetRect(new Rectangle(panel.PointToScreen(Point.Empty), panel.Size));
        _panel.Draw += PanelDrawCompleted;
        _panel.Invalidated += SourceInvalidated;
        _menu.Invalidated += SourceInvalidated;
    }

    internal void ShowFlyout()
    {
        _ = Handle;
        RenderFrame();
        Show(PARENT!);
    }

    /// <inheritdoc />
    public override Bitmap PrintBit()
    {
        _panelDrawCompleted = false;
        Bitmap bitmap = _panel.DrawBitmap()
            ?? throw new InvalidOperationException("The navigation flyout has no drawable bounds.");
        try
        {
            // AntdUI.DrawBitmap catches draw failures; its final Draw event proves the panel completed.
            if (!_panelDrawCompleted)
            {
                throw new InvalidOperationException("AntdUI could not draw the navigation flyout panel.");
            }

            using var menuBitmap = new Bitmap(_menu.Width, _menu.Height);
            using (Canvas canvas = Graphics.FromImage(menuBitmap).High(_menu.Dpi))
            {
                _menu.DrawFlyout(canvas);
            }

            // Native Menu resets its canvas transform; keep that local to the menu-sized frame.
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawImageUnscaled(menuBitmap, _menu.Location);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs e)
    {
        DispatchCallback(() =>
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                Capture = true;
            }

            _menu.FlyoutMouseDown(ToMenuCoordinates(e));
        });
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        DispatchCallback(() =>
        {
            base.OnMouseMove(e);
            if (Capture && !_menu.Bounds.Contains(e.Location))
            {
                _menu.FlyoutMouseLeave();
            }

            MouseEventArgs translated = ToMenuCoordinates(e);
            _menu.FlyoutMouseMove(translated);
            Cursor = _menu.HitTest(translated.X, translated.Y) is { Enabled: true } ? Cursors.Hand : Cursors.Default;
        });
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs e)
    {
        DispatchCallback(() =>
        {
            base.OnMouseUp(e);
            Capture = false;
            _menu.FlyoutMouseUp(ToMenuCoordinates(e));
        });
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        DispatchCallback(() =>
        {
            base.OnMouseLeave(e);
            _menu.FlyoutMouseLeave();
        });
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        DispatchCallback(() =>
        {
            base.OnMouseWheel(e);
            _menu.FlyoutMouseWheel(ToMenuCoordinates(e));
        });
    }

    /// <inheritdoc />
    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        _callbackFailure = null;
        base.WndProc(ref m);
        // ILayeredForm catches callback exceptions; restore the application's fail-fast policy.
        _callbackFailure?.Throw();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closed = true;
            _panel.Draw -= PanelDrawCompleted;
            _panel.Invalidated -= SourceInvalidated;
            _menu.Invalidated -= SourceInvalidated;
        }

        base.Dispose(disposing);
    }

    private void PanelDrawCompleted(object sender, DrawEventArgs e) => _panelDrawCompleted = true;

    private void SourceInvalidated(object? sender, InvalidateEventArgs e)
    {
        // Native hover animation runs on a worker; only the panel's own shadow repaint is redundant.
        if ((_rendering && sender == _panel) || _closed || !IsHandleCreated
            || Interlocked.Exchange(ref _framePending, 1) != 0)
        {
            return;
        }

        _uiContext.Post(_ =>
        {
            Interlocked.Exchange(ref _framePending, 0);
            if (!_closed)
            {
                RenderFrame();
            }
        }, null);
    }

    private void RenderFrame()
    {
        _rendering = true;
        try
        {
            // Printmap releases AntdUI's preceding frame and does not catch our draw exceptions.
            using Bitmap bitmap = Printmap()
                ?? throw new InvalidOperationException("The navigation flyout produced no frame.");
            Win32.RenderResult result = Print(bitmap);
            if (result != Win32.RenderResult.OK)
            {
                throw new InvalidOperationException($"The navigation flyout could not be presented: {result}.");
            }
        }
        finally
        {
            _rendering = false;
        }
    }

    private MouseEventArgs ToMenuCoordinates(MouseEventArgs e)
        => new(e.Button, e.Clicks, e.X - _menu.Left, e.Y - _menu.Top, e.Delta);

    private void DispatchCallback(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            _callbackFailure = ExceptionDispatchInfo.Capture(exception);
            throw;
        }
    }
}
