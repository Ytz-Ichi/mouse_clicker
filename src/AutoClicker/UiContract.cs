namespace AutoClicker;

/// <summary>
/// ui_contract.json (v1.0.0) の定義を C# 定数として保持する単一ソース。
/// UI 文言・初期値・制約はすべてここを経由して参照する。
/// JSON ファイルが仕様凍結の正であり、本クラスと矛盾がある場合は JSON を優先すること。
/// </summary>
internal static class UiContract
{
    // ── Labels ──────────────────────────────────────────
    internal const string StatusPrefix       = "状態: ";
    internal const string StatusStopped      = "停止中";
    internal const string StatusRunning      = "実行中";
    internal const string ModePrefix         = "モード: ";
    internal const string ModeSingle         = "単一点";
    internal const string ModeMulti          = "複数点";
    internal const string SingleCoordinates  = "座標";
    internal const string MultiPointsLabel   = "座標リスト";
    internal const string NoteNegativeCoords = "※ マルチモニタ環境では座標が負になる場合があります。";

    // ── Defaults ────────────────────────────────────────
    internal const string DefaultMode        = "single";
    internal const string DefaultClickType   = "left";
    internal const int    DefaultIntervalMs  = 100;
    internal const string DefaultCountMode   = "infinite";
    internal const int    DefaultCountFixed  = 100;
    internal const int    DefaultSingleX     = 0;
    internal const int    DefaultSingleY     = 0;
    internal const int    DefaultExtraWaitMs = 0;
    internal const bool   DefaultTrayEnabled = true;
    internal const string DefaultHotkeyToggle = "F6";
    internal const string DefaultHotkeyStop   = "F7";

    // ── Limits ──────────────────────────────────────────
    internal const int IntervalMsMin   = 1;
    internal const int IntervalMsMax   = 600_000;
    internal const int CountFixedMin   = 1;
    internal const int CountFixedMax   = 1_000_000;
    internal const int ExtraWaitMsMin  = 0;
    internal const int ExtraWaitMsMax  = 600_000;

    // ── Errors（ui_contract.json "errors" セクション）──
    internal const string ErrorInvalidInput     = "入力値を確認してください。";
    internal const string ErrorHotkeyConflict   = "ホットキーが重複しています。別のキーにしてください。";
    internal const string ErrorHotkeyDisallowed = "ホットキーとして使用できないキーです。";

    // ── Helpers ─────────────────────────────────────────
    internal static int ClampInterval(int v) =>
        Math.Clamp(v, IntervalMsMin, IntervalMsMax);

    internal static int ClampCountFixed(int v) =>
        Math.Clamp(v, CountFixedMin, CountFixedMax);

    internal static int ClampExtraWait(int v) =>
        Math.Clamp(v, ExtraWaitMsMin, ExtraWaitMsMax);
}
