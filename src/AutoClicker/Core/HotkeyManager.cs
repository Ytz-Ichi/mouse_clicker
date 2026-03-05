using System.Windows.Input;
using System.Windows.Interop;
using AutoClicker.Native;

namespace AutoClicker.Core;

/// <summary>
/// グローバルホットキーの登録・解除を管理する。
/// RegisterHotKey / UnregisterHotKey を使用。
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int HOTKEY_ID_TOGGLE = 1;
    private const int HOTKEY_ID_STOP = 2;

    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _suspended;

    private Key _toggleKey = Key.F6;
    private Key _stopKey = Key.F7;

    public Key ToggleKey => _toggleKey;
    public Key StopKey => _stopKey;

    public event Action? ToggleRequested;
    public event Action? StopRequested;

    /// <summary>予約キー（使用禁止）。</summary>
    private static readonly HashSet<Key> ReservedKeys = [Key.LWin, Key.RWin];

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);
        RegisterAll();
    }

    // ── 一時停止/再開（ホットキー変更キャプチャ用）──

    /// <summary>
    /// ホットキーを一時解除する。キャプチャモード中に呼ぶ。
    /// </summary>
    public void Suspend()
    {
        _suspended = true;
        UnregisterAll();
    }

    /// <summary>
    /// ホットキーを再登録する。キャプチャモード終了時に呼ぶ。
    /// </summary>
    public void Resume()
    {
        _suspended = false;
        RegisterAll();
    }

    // ── キー変更 ──

    public bool TrySetToggleKey(Key key, out string? error)
    {
        error = null;
        if (ReservedKeys.Contains(key))
        {
            error = "reserved";
            return false;
        }
        if (key == _stopKey)
        {
            error = "conflict";
            return false;
        }

        var oldKey = _toggleKey;
        UnregisterAll();
        _toggleKey = key;

        if (!RegisterAll())
        {
            // 登録失敗 → ロールバック
            _toggleKey = oldKey;
            RegisterAll();
            error = "register_failed";
            return false;
        }
        return true;
    }

    public bool TrySetStopKey(Key key, out string? error)
    {
        error = null;
        if (ReservedKeys.Contains(key))
        {
            error = "reserved";
            return false;
        }
        if (key == _toggleKey)
        {
            error = "conflict";
            return false;
        }

        var oldKey = _stopKey;
        UnregisterAll();
        _stopKey = key;

        if (!RegisterAll())
        {
            _stopKey = oldKey;
            RegisterAll();
            error = "register_failed";
            return false;
        }
        return true;
    }

    /// <summary>
    /// 両方のキーを設定する。バリデーション付き。
    /// 不正な場合はデフォルト (F6/F7) にフォールバック。
    /// </summary>
    public void SetKeys(Key toggleKey, Key stopKey)
    {
        UnregisterAll();

        // バリデーション: 予約キー・重複 → デフォルトへフォールバック
        if (ReservedKeys.Contains(toggleKey) || ReservedKeys.Contains(stopKey)
            || toggleKey == stopKey)
        {
            toggleKey = Key.F6;
            stopKey = Key.F7;
        }

        _toggleKey = toggleKey;
        _stopKey = stopKey;

        if (!RegisterAll())
        {
            // 登録失敗 → デフォルトにフォールバック
            _toggleKey = Key.F6;
            _stopKey = Key.F7;
            RegisterAll();
        }
    }

    // ── 内部 ──

    private bool RegisterAll()
    {
        if (_hwnd == IntPtr.Zero) return true;
        if (_suspended) return true;
        bool ok1 = Register(HOTKEY_ID_TOGGLE, _toggleKey);
        bool ok2 = Register(HOTKEY_ID_STOP, _stopKey);
        return ok1 && ok2;
    }

    private void UnregisterAll()
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_TOGGLE);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_STOP);
    }

    private bool Register(int id, Key key)
    {
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return NativeMethods.RegisterHotKey(_hwnd, id, 0, vk);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && !_suspended)
        {
            int id = wParam.ToInt32();
            if (id == HOTKEY_ID_TOGGLE)
            {
                ToggleRequested?.Invoke();
                handled = true;
            }
            else if (id == HOTKEY_ID_STOP)
            {
                StopRequested?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
    }
}
