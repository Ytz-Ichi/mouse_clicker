namespace AutoClicker.Native;

internal static class CursorPositionProvider
{
    internal static (int X, int Y) GetCurrentPosition()
    {
        NativeMethods.GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }
}
