using System.Runtime.InteropServices;

namespace AutoClicker.Native;

internal static class SendInputWrapper
{
    /// <summary>
    /// 指定座標でマウスクリックを送出する。
    /// 必ず専用ワーカースレッドから呼ぶこと（UIスレッド/ホットキーハンドラから直接呼ばない）。
    /// </summary>
    internal static void Click(Models.ClickType clickType, int screenX, int screenY)
    {
        var (absX, absY) = ToAbsolute(screenX, screenY);
        GetMouseFlags(clickType, out uint downFlag, out uint upFlag);

        var inputs = new NativeMethods.INPUT[3];
        int size = Marshal.SizeOf<NativeMethods.INPUT>();

        // 1) Move to absolute position
        inputs[0].type = NativeMethods.INPUT_MOUSE;
        inputs[0].mi.dx = absX;
        inputs[0].mi.dy = absY;
        inputs[0].mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE
                              | NativeMethods.MOUSEEVENTF_ABSOLUTE
                              | NativeMethods.MOUSEEVENTF_VIRTUALDESK;

        // 2) Mouse down
        inputs[1].type = NativeMethods.INPUT_MOUSE;
        inputs[1].mi.dwFlags = downFlag;

        // 3) Mouse up
        inputs[2].type = NativeMethods.INPUT_MOUSE;
        inputs[2].mi.dwFlags = upFlag;

        NativeMethods.SendInput(3, inputs, size);
    }

    private static (int absX, int absY) ToAbsolute(int screenX, int screenY)
    {
        int vLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int vTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        int vWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int vHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        if (vWidth <= 1) vWidth = 2;
        if (vHeight <= 1) vHeight = 2;

        int absX = (int)(((long)(screenX - vLeft) * 65535) / (vWidth - 1));
        int absY = (int)(((long)(screenY - vTop) * 65535) / (vHeight - 1));
        return (absX, absY);
    }

    private static void GetMouseFlags(Models.ClickType clickType, out uint down, out uint up)
    {
        switch (clickType)
        {
            case Models.ClickType.Right:
                down = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                up = NativeMethods.MOUSEEVENTF_RIGHTUP;
                break;
            case Models.ClickType.Middle:
                down = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                up = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                break;
            default:
                down = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                up = NativeMethods.MOUSEEVENTF_LEFTUP;
                break;
        }
    }
}
