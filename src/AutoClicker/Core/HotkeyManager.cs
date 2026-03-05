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

    private Key _toggleKey = Key.F6;
    private Key _stopKey = Key.F7;

    public Key ToggleKey => _toggleKey;
    public Key StopKey => _stopKey;

    public event Action? ToggleRequested;
    public event Action? StopRequested;

    /// <summary>
    /// 予約キー（使用禁止）の判定。
    /// </summary>
    private static readonly HashSet<Key> ReservedKeys = [Key.LWin, Key.RWin];

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);

        RegisterAll();
    }

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

        UnregisterAll();
        _toggleKey = key;
        RegisterAll();
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

        UnregisterAll();
        _stopKey = key;
        RegisterAll();
        return true;
    }

    public void SetKeys(Key toggleKey, Key stopKey)
    {
        UnregisterAll();
        _toggleKey = toggleKey;
        _stopKey = stopKey;
        RegisterAll();
    }

    private void RegisterAll()
    {
        if (_hwnd == IntPtr.Zero) return;
        Register(HOTKEY_ID_TOGGLE, _toggleKey);
        Register(HOTKEY_ID_STOP, _stopKey);
    }

    private void UnregisterAll()
    {
        if (_hwnd == IntPtr.Zero) return;
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_TOGGLE);
        NativeMethods.UnregisterHotKey(_hwnd, HOTKEY_ID_STOP);
    }

    private void Register(int id, Key key)
    {
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        NativeMethods.RegisterHotKey(_hwnd, id, 0, vk);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
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
