using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using AutoClicker.ViewModels;
using WinFormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using WinFormsContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using WinFormsSeparator = System.Windows.Forms.ToolStripSeparator;
using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace AutoClicker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private WinFormsNotifyIcon? _trayIcon;

    public MainViewModel ViewModel => _vm;

    public MainWindow()
    {
        _vm = new MainViewModel();
        DataContext = _vm;

        InitializeComponent();

        Loaded += OnLoaded;
        SetupTrayIcon();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        _vm.InitializeHotkeys(hwnd);
    }

    // --- Tray icon ---
    private void SetupTrayIcon()
    {
        _trayIcon = new WinFormsNotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Auto Clicker (Safe)",
            Visible = true
        };

        var menu = new WinFormsContextMenuStrip();
        menu.Items.Add("表示", null, (_, _) => ShowWindow());
        menu.Items.Add("開始/停止", null, (_, _) =>
        {
            if (_vm.IsRunning)
                _vm.StopCommand.Execute(null);
            else
                _vm.StartCommand.Execute(null);
        });
        menu.Items.Add("停止", null, (_, _) => _vm.StopCommand.Execute(null));
        menu.Items.Add(new WinFormsSeparator());
        menu.Items.Add("終了", null, (_, _) => _vm.ExitCommand.Execute(null));

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => ShowWindow();
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        var bmp = new System.Drawing.Bitmap(32, 32);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.Transparent);
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.DodgerBlue);
            g.FillEllipse(brush, 2, 2, 28, 28);
            using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
            g.DrawLine(pen, 12, 8, 12, 24);
            g.DrawLine(pen, 12, 24, 16, 20);
            g.DrawLine(pen, 12, 24, 8, 20);
        }
        var hIcon = bmp.GetHicon();
        return System.Drawing.Icon.FromHandle(hIcon);
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    // --- Window events ---
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_vm.IsResidentTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _vm.ForceStop();
            _vm.SaveSettings();
            CleanupTray();
        }
    }

    private void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (_vm.IsCapturingToggleHotkey || _vm.IsCapturingStopHotkey)
        {
            var key = e.Key == WpfKey.System ? e.SystemKey : e.Key;
            _vm.HandleHotkeyCapture(key);
            e.Handled = true;
        }
    }

    public void ForceClose()
    {
        _vm.ForceStop();
        _vm.SaveSettings();
        CleanupTray();
        Closing -= Window_Closing;
        Close();
    }

    private void CleanupTray()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
