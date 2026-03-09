using AutoClicker.Models;
using AutoClicker.Native;

namespace AutoClicker.Core;

/// <summary>
/// クリック送出専用ワーカー。
/// SendInput はすべてこのクラスの内部 Task 上で実行される。
/// SemaphoreSlim(1,1) で多重起動を防止する。
/// </summary>
public sealed class ClickEngine : IDisposable
{
    public enum State { Stopped, Running, Stopping }

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ManualResetEventSlim _completionEvent = new(true); // true = not running
    private CancellationTokenSource? _cts;
    private volatile State _state = State.Stopped;

    public State CurrentState => _state;

    /// <summary>UIスレッドへの通知用。Dispatcher経由で購読すること。</summary>
    public event Action<State>? StateChanged;

    /// <summary>
    /// 実行を開始する。すでに実行中の場合は何もしない（多重起動禁止）。
    /// </summary>
    public bool TryStart(ConfigSnapshot config)
    {
        if (!_semaphore.Wait(0))
            return false; // Already running

        _cts = new CancellationTokenSource();
        _completionEvent.Reset();
        SetState(State.Running);

        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await RunLoopAsync(config, token);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception)
            {
                // Safety: any exception returns to Stopped
            }
            finally
            {
                SetState(State.Stopped);
                _cts?.Dispose();
                _cts = null;
                _semaphore.Release();
                _completionEvent.Set(); // Signal AFTER semaphore release
            }
        });

        return true;
    }

    /// <summary>
    /// 停止要求。CancellationToken経由で1間隔以内に停止する。
    /// </summary>
    public void Stop()
    {
        if (_state == State.Running)
        {
            SetState(State.Stopping);
        }

        // ステートに関係なく常にキャンセルを試行する。
        // 既に Stopping/Stopped でも CTS が生きていれば確実に止める。
        try { _cts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    private void SetState(State newState)
    {
        _state = newState;
        StateChanged?.Invoke(newState);
    }

    private async Task RunLoopAsync(ConfigSnapshot config, CancellationToken ct)
    {
        if (config.IsSingleMode)
            await RunSinglePointLoopAsync(config, ct);
        else
            await RunMultiPointLoopAsync(config, ct);
    }

    private static async Task RunSinglePointLoopAsync(ConfigSnapshot config, CancellationToken ct)
    {
        int done = 0;
        while (!ct.IsCancellationRequested)
        {
            SendInputWrapper.Click(config.ClickType, config.SingleX, config.SingleY);
            done++;

            if (!config.IsInfinite && done >= config.Count)
                break;

            // Cancellable delay — stops within 1 interval
            await Task.Delay(config.IntervalMs, ct);
        }
    }

    private static async Task RunMultiPointLoopAsync(ConfigSnapshot config, CancellationToken ct)
    {
        if (config.Points.Count == 0) return;

        int cycles = 0;
        while (!ct.IsCancellationRequested)
        {
            foreach (var pt in config.Points)
            {
                ct.ThrowIfCancellationRequested();

                SendInputWrapper.Click(config.ClickType, pt.X, pt.Y);

                if (pt.ExtraWaitMs > 0)
                    await Task.Delay(pt.ExtraWaitMs, ct);

                await Task.Delay(config.IntervalMs, ct);
            }

            cycles++;
            if (!config.IsInfinite && cycles >= config.Count)
                break;
        }
    }

    public void Dispose()
    {
        Stop();
        // ワーカー完了を待ってからリソースを破棄する（最大5秒）
        _completionEvent.Wait(TimeSpan.FromSeconds(5));
        _cts?.Dispose();
        _semaphore.Dispose();
        _completionEvent.Dispose();
    }
}
