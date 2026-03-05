using System.Collections.ObjectModel;
using System.ComponentModel;
using AutoClicker.Core;
using AutoClicker.Models;
using AutoClicker.Native;
using WpfApplication = System.Windows.Application;
using Key = System.Windows.Input.Key;
using ICommand = System.Windows.Input.ICommand;

namespace AutoClicker.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ClickEngine _engine = new();
    private readonly HotkeyManager _hotkeyManager = new();

    // --- State ---
    private string _statusText = UiContract.StatusStopped;
    private bool _isRunning;
    private bool _isSingleMode = true;
    private int _clickTypeIndex; // 0=left, 1=right, 2=middle
    private string _intervalMs = UiContract.DefaultIntervalMs.ToString();
    private bool _isInfinite = true;
    private string _fixedCount = UiContract.DefaultCountFixed.ToString();
    private string _singleX = UiContract.DefaultSingleX.ToString();
    private string _singleY = UiContract.DefaultSingleY.ToString();
    private int _selectedPointIndex = -1;
    private string _toggleHotkeyText = UiContract.DefaultHotkeyToggle;
    private string _stopHotkeyText = UiContract.DefaultHotkeyStop;
    private bool _isResidentTray = UiContract.DefaultTrayEnabled;
    private bool _isCapturingToggleHotkey;
    private bool _isCapturingStopHotkey;
    private bool _isCapturing3s;
    private string? _errorMessage;

    // --- Properties ---
    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(nameof(StatusText)); }
    }

    public string ModeText => IsSingleMode ? UiContract.ModeSingle : UiContract.ModeMulti;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            _isRunning = value;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsStopped));
        }
    }

    public bool IsStopped => !_isRunning;

    public bool IsSingleMode
    {
        get => _isSingleMode;
        set
        {
            _isSingleMode = value;
            OnPropertyChanged(nameof(IsSingleMode));
            OnPropertyChanged(nameof(IsMultiMode));
            OnPropertyChanged(nameof(ModeText));
        }
    }

    public bool IsMultiMode
    {
        get => !_isSingleMode;
        set => IsSingleMode = !value;
    }

    public int ClickTypeIndex
    {
        get => _clickTypeIndex;
        set { _clickTypeIndex = value; OnPropertyChanged(nameof(ClickTypeIndex)); }
    }

    public string IntervalMs
    {
        get => _intervalMs;
        set { _intervalMs = value; OnPropertyChanged(nameof(IntervalMs)); }
    }

    public bool IsInfinite
    {
        get => _isInfinite;
        set
        {
            _isInfinite = value;
            OnPropertyChanged(nameof(IsInfinite));
            OnPropertyChanged(nameof(IsFixedCount));
        }
    }

    public bool IsFixedCount
    {
        get => !_isInfinite;
        set => IsInfinite = !value;
    }

    public string FixedCount
    {
        get => _fixedCount;
        set { _fixedCount = value; OnPropertyChanged(nameof(FixedCount)); }
    }

    public string SingleX
    {
        get => _singleX;
        set { _singleX = value; OnPropertyChanged(nameof(SingleX)); }
    }

    public string SingleY
    {
        get => _singleY;
        set { _singleY = value; OnPropertyChanged(nameof(SingleY)); }
    }

    public ObservableCollection<ClickPoint> MultiPoints { get; } = [];

    public int SelectedPointIndex
    {
        get => _selectedPointIndex;
        set
        {
            _selectedPointIndex = value;
            OnPropertyChanged(nameof(SelectedPointIndex));
            OnPropertyChanged(nameof(HasSelectedPoint));
        }
    }

    public bool HasSelectedPoint => _selectedPointIndex >= 0 && _selectedPointIndex < MultiPoints.Count;

    public string ToggleHotkeyText
    {
        get => _toggleHotkeyText;
        private set { _toggleHotkeyText = value; OnPropertyChanged(nameof(ToggleHotkeyText)); }
    }

    public string StopHotkeyText
    {
        get => _stopHotkeyText;
        private set { _stopHotkeyText = value; OnPropertyChanged(nameof(StopHotkeyText)); }
    }

    public bool IsResidentTray
    {
        get => _isResidentTray;
        set { _isResidentTray = value; OnPropertyChanged(nameof(IsResidentTray)); }
    }

    public bool IsCapturingToggleHotkey
    {
        get => _isCapturingToggleHotkey;
        set { _isCapturingToggleHotkey = value; OnPropertyChanged(nameof(IsCapturingToggleHotkey)); }
    }

    public bool IsCapturingStopHotkey
    {
        get => _isCapturingStopHotkey;
        set { _isCapturingStopHotkey = value; OnPropertyChanged(nameof(IsCapturingStopHotkey)); }
    }

    public bool IsCapturing3s
    {
        get => _isCapturing3s;
        private set { _isCapturing3s = value; OnPropertyChanged(nameof(IsCapturing3s)); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
    }

    // --- Commands ---
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand GetPositionNowCommand { get; }
    public RelayCommand GetPosition3sCommand { get; }
    public RelayCommand AddPointNowCommand { get; }
    public RelayCommand AddPoint3sCommand { get; }
    public RelayCommand DeletePointCommand { get; }
    public RelayCommand MovePointUpCommand { get; }
    public RelayCommand MovePointDownCommand { get; }
    public RelayCommand ChangeToggleHotkeyCommand { get; }
    public RelayCommand ChangeStopHotkeyCommand { get; }

    public HotkeyManager HotkeyManager => _hotkeyManager;

    public MainViewModel()
    {
        StartCommand = new RelayCommand(ExecuteStart, () => IsStopped);
        StopCommand = new RelayCommand(ExecuteStop, () => IsRunning);
        ExitCommand = new RelayCommand(ExecuteExit);
        GetPositionNowCommand = new RelayCommand(ExecuteGetPositionNow, () => IsStopped);
        GetPosition3sCommand = new RelayCommand(ExecuteGetPosition3s, () => IsStopped && !IsCapturing3s);
        AddPointNowCommand = new RelayCommand(ExecuteAddPointNow, () => IsStopped);
        AddPoint3sCommand = new RelayCommand(ExecuteAddPoint3s, () => IsStopped && !IsCapturing3s);
        DeletePointCommand = new RelayCommand(ExecuteDeletePoint, () => IsStopped && HasSelectedPoint);
        MovePointUpCommand = new RelayCommand(ExecuteMovePointUp, () => IsStopped && HasSelectedPoint && SelectedPointIndex > 0);
        MovePointDownCommand = new RelayCommand(ExecuteMovePointDown, () => IsStopped && HasSelectedPoint && SelectedPointIndex < MultiPoints.Count - 1);
        ChangeToggleHotkeyCommand = new RelayCommand(EnterToggleCapture);
        ChangeStopHotkeyCommand = new RelayCommand(EnterStopCapture);

        _engine.StateChanged += OnEngineStateChanged;
        _hotkeyManager.ToggleRequested += OnToggleRequested;
        _hotkeyManager.StopRequested += OnStopRequested;

        LoadSettings();
    }

    // ── ホットキーキャプチャ開始（Suspend で HK 一時解除）──

    private void EnterToggleCapture()
    {
        _hotkeyManager.Suspend();
        IsCapturingToggleHotkey = true;
        IsCapturingStopHotkey = false;
    }

    private void EnterStopCapture()
    {
        _hotkeyManager.Suspend();
        IsCapturingStopHotkey = true;
        IsCapturingToggleHotkey = false;
    }

    // --- Engine event handlers ---
    private void OnEngineStateChanged(ClickEngine.State state)
    {
        // BeginInvoke を使い、Dispose 時のデッドロックを防止する
        void UpdateUi()
        {
            bool running = state == ClickEngine.State.Running;
            IsRunning = running;
            StatusText = running ? UiContract.StatusRunning : UiContract.StatusStopped;
            ErrorMessage = null;
        }

        var dispatcher = WpfApplication.Current?.Dispatcher;
        if (dispatcher == null) return;

        if (dispatcher.CheckAccess())
            UpdateUi();
        else
            dispatcher.BeginInvoke(UpdateUi);
    }

    private void OnToggleRequested()
    {
        var dispatcher = WpfApplication.Current?.Dispatcher;
        if (dispatcher == null) return;
        dispatcher.BeginInvoke(() =>
        {
            // キャプチャ中はホットキーイベントを無視
            if (IsCapturingToggleHotkey || IsCapturingStopHotkey) return;

            if (IsRunning)
                ExecuteStop();
            else
                ExecuteStart();
        });
    }

    private void OnStopRequested()
    {
        var dispatcher = WpfApplication.Current?.Dispatcher;
        if (dispatcher == null) return;
        dispatcher.BeginInvoke(() =>
        {
            if (IsCapturingToggleHotkey || IsCapturingStopHotkey) return;

            if (IsRunning)
                ExecuteStop();
        });
    }

    // --- Command implementations ---
    private void ExecuteStart()
    {
        ErrorMessage = null;

        if (!ValidateInputs(out string? err))
        {
            ErrorMessage = err;
            return;
        }

        var clickType = _clickTypeIndex switch
        {
            1 => ClickType.Right,
            2 => ClickType.Middle,
            _ => ClickType.Left
        };

        int interval = int.Parse(_intervalMs);
        bool infinite = _isInfinite;
        int count = infinite ? 0 : int.Parse(_fixedCount);
        int sx = int.Parse(_singleX);
        int sy = int.Parse(_singleY);

        var snapshot = new ConfigSnapshot(
            _isSingleMode,
            clickType,
            interval,
            infinite,
            count,
            sx,
            sy,
            MultiPoints);

        _engine.TryStart(snapshot);
    }

    private void ExecuteStop()
    {
        _engine.Stop();
    }

    private void ExecuteExit()
    {
        _engine.Stop();
        SaveSettings();
        WpfApplication.Current.Shutdown();
    }

    private void ExecuteGetPositionNow()
    {
        var (x, y) = CursorPositionProvider.GetCurrentPosition();
        SingleX = x.ToString();
        SingleY = y.ToString();
    }

    private async void ExecuteGetPosition3s()
    {
        IsCapturing3s = true;
        try
        {
            await Task.Delay(3000);
            var (x, y) = CursorPositionProvider.GetCurrentPosition();
            SingleX = x.ToString();
            SingleY = y.ToString();
        }
        finally
        {
            IsCapturing3s = false;
        }
    }

    private void ExecuteAddPointNow()
    {
        var (x, y) = CursorPositionProvider.GetCurrentPosition();
        MultiPoints.Add(new ClickPoint(x, y, UiContract.DefaultExtraWaitMs));
    }

    private async void ExecuteAddPoint3s()
    {
        IsCapturing3s = true;
        try
        {
            await Task.Delay(3000);
            var (x, y) = CursorPositionProvider.GetCurrentPosition();
            MultiPoints.Add(new ClickPoint(x, y, UiContract.DefaultExtraWaitMs));
        }
        finally
        {
            IsCapturing3s = false;
        }
    }

    private void ExecuteDeletePoint()
    {
        if (SelectedPointIndex >= 0 && SelectedPointIndex < MultiPoints.Count)
        {
            int idx = SelectedPointIndex;
            MultiPoints.RemoveAt(idx);
            if (MultiPoints.Count > 0)
                SelectedPointIndex = Math.Min(idx, MultiPoints.Count - 1);
            else
                SelectedPointIndex = -1;
        }
    }

    private void ExecuteMovePointUp()
    {
        int idx = SelectedPointIndex;
        if (idx > 0)
        {
            MultiPoints.Move(idx, idx - 1);
            SelectedPointIndex = idx - 1;
        }
    }

    private void ExecuteMovePointDown()
    {
        int idx = SelectedPointIndex;
        if (idx < MultiPoints.Count - 1)
        {
            MultiPoints.Move(idx, idx + 1);
            SelectedPointIndex = idx + 1;
        }
    }

    /// <summary>
    /// ホットキー変更中にキーが押されたときの処理。
    /// Window の PreviewKeyDown から呼ばれる。
    /// 終了後に必ず Resume して HK を再登録する。
    /// </summary>
    public void HandleHotkeyCapture(Key key)
    {
        if (key == Key.Escape)
        {
            IsCapturingToggleHotkey = false;
            IsCapturingStopHotkey = false;
            _hotkeyManager.Resume();
            return;
        }

        if (IsCapturingToggleHotkey)
        {
            if (_hotkeyManager.TrySetToggleKey(key, out string? error))
            {
                ToggleHotkeyText = KeyToString(key);
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = error == "conflict"
                    ? UiContract.ErrorHotkeyConflict
                    : UiContract.ErrorHotkeyDisallowed;
            }
            IsCapturingToggleHotkey = false;
            // Resume は TrySetToggleKey 内で RegisterAll 済みなので不要だが安全のため
            _hotkeyManager.Resume();
        }
        else if (IsCapturingStopHotkey)
        {
            if (_hotkeyManager.TrySetStopKey(key, out string? error))
            {
                StopHotkeyText = KeyToString(key);
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = error == "conflict"
                    ? UiContract.ErrorHotkeyConflict
                    : UiContract.ErrorHotkeyDisallowed;
            }
            IsCapturingStopHotkey = false;
            _hotkeyManager.Resume();
        }
    }

    // --- Validation ---
    private bool ValidateInputs(out string? error)
    {
        error = UiContract.ErrorInvalidInput;

        if (!int.TryParse(_intervalMs, out int interval)
            || interval < UiContract.IntervalMsMin
            || interval > UiContract.IntervalMsMax)
            return false;

        if (!_isInfinite)
        {
            if (!int.TryParse(_fixedCount, out int count)
                || count < UiContract.CountFixedMin
                || count > UiContract.CountFixedMax)
                return false;
        }

        if (_isSingleMode)
        {
            if (!int.TryParse(_singleX, out _) || !int.TryParse(_singleY, out _))
                return false;
        }
        else
        {
            if (MultiPoints.Count == 0)
                return false;

            foreach (var pt in MultiPoints)
            {
                if (pt.ExtraWaitMs < UiContract.ExtraWaitMsMin
                    || pt.ExtraWaitMs > UiContract.ExtraWaitMsMax)
                    return false;
            }
        }

        error = null;
        return true;
    }

    // --- Settings persistence ---
    private void LoadSettings()
    {
        var s = SettingsStore.Load();

        IsSingleMode = s.Mode == "single";
        ClickTypeIndex = s.ClickTypeName switch
        {
            "right" => 1,
            "middle" => 2,
            _ => 0
        };

        // 範囲クランプ付きで復元
        int interval = UiContract.ClampInterval(s.IntervalMs);
        IntervalMs = interval.ToString();

        IsInfinite = s.CountMode == "infinite";
        int fixedCount = UiContract.ClampCountFixed(s.CountFixed);
        FixedCount = fixedCount.ToString();

        SingleX = s.SingleX.ToString();
        SingleY = s.SingleY.ToString();
        IsResidentTray = s.ResidentTray;

        MultiPoints.Clear();
        foreach (var p in s.MultiPoints)
        {
            int ew = UiContract.ClampExtraWait(p.ExtraWaitMs);
            MultiPoints.Add(new ClickPoint(p.X, p.Y, ew));
        }

        // ホットキー復元 — バリデーション付きフォールバック
        Key toggleKey = Key.F6;
        Key stopKey = Key.F7;

        if (Enum.TryParse<Key>(s.HotkeyToggle, out var tk))
            toggleKey = tk;
        if (Enum.TryParse<Key>(s.HotkeyStop, out var sk))
            stopKey = sk;

        // 予約キーや重複は SetKeys 内部でデフォルトへフォールバックされる
        _pendingToggleKey = toggleKey;
        _pendingStopKey = stopKey;
    }

    private Key _pendingToggleKey = Key.F6;
    private Key _pendingStopKey = Key.F7;

    public void InitializeHotkeys(IntPtr hwnd)
    {
        _hotkeyManager.SetKeys(_pendingToggleKey, _pendingStopKey);
        _hotkeyManager.Initialize(hwnd);
        ToggleHotkeyText = KeyToString(_hotkeyManager.ToggleKey);
        StopHotkeyText = KeyToString(_hotkeyManager.StopKey);
    }

    public void SaveSettings()
    {
        // 保存時も範囲クランプ
        int intervalVal = int.TryParse(_intervalMs, out int iv)
            ? UiContract.ClampInterval(iv)
            : UiContract.DefaultIntervalMs;
        int countFixedVal = int.TryParse(_fixedCount, out int fc)
            ? UiContract.ClampCountFixed(fc)
            : UiContract.DefaultCountFixed;

        var s = new AppSettings
        {
            Mode = _isSingleMode ? "single" : "multi",
            ClickTypeName = _clickTypeIndex switch
            {
                1 => "right",
                2 => "middle",
                _ => "left"
            },
            IntervalMs = intervalVal,
            CountMode = _isInfinite ? "infinite" : "fixed",
            CountFixed = countFixedVal,
            SingleX = int.TryParse(_singleX, out int sx) ? sx : UiContract.DefaultSingleX,
            SingleY = int.TryParse(_singleY, out int sy) ? sy : UiContract.DefaultSingleY,
            ResidentTray = _isResidentTray,
            HotkeyToggle = _hotkeyManager.ToggleKey.ToString(),
            HotkeyStop = _hotkeyManager.StopKey.ToString(),
            MultiPoints = MultiPoints.Select(p => new ClickPointData
            {
                X = p.X,
                Y = p.Y,
                ExtraWaitMs = UiContract.ClampExtraWait(p.ExtraWaitMs)
            }).ToList()
        };

        SettingsStore.Save(s);
    }

    public void ForceStop()
    {
        _engine.Stop();
    }

    private static string KeyToString(Key key)
    {
        return key.ToString();
    }

    public void Dispose()
    {
        SaveSettings();
        _engine.Dispose();
        _hotkeyManager.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
