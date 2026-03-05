namespace AutoClicker.Models;

/// <summary>
/// 実行開始時点の設定スナップショット（不変）。
/// ワーカーはこのスナップショットだけを参照する。
/// </summary>
public sealed class ConfigSnapshot
{
    public bool IsSingleMode { get; }
    public ClickType ClickType { get; }
    public int IntervalMs { get; }
    public bool IsInfinite { get; }
    public int Count { get; }
    public int SingleX { get; }
    public int SingleY { get; }
    public IReadOnlyList<ClickPointSnapshot> Points { get; }

    public ConfigSnapshot(
        bool isSingleMode,
        ClickType clickType,
        int intervalMs,
        bool isInfinite,
        int count,
        int singleX,
        int singleY,
        IEnumerable<ClickPoint> points)
    {
        IsSingleMode = isSingleMode;
        ClickType = clickType;
        IntervalMs = intervalMs;
        IsInfinite = isInfinite;
        Count = count;
        SingleX = singleX;
        SingleY = singleY;
        Points = points.Select(p => new ClickPointSnapshot(p.X, p.Y, p.ExtraWaitMs)).ToList();
    }
}

public sealed record ClickPointSnapshot(int X, int Y, int ExtraWaitMs);
