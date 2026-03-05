using System.ComponentModel;

namespace AutoClicker.Models;

public class ClickPoint : INotifyPropertyChanged
{
    private int _x;
    private int _y;
    private int _extraWaitMs;

    public int X
    {
        get => _x;
        set { _x = value; OnPropertyChanged(nameof(X)); }
    }

    public int Y
    {
        get => _y;
        set { _y = value; OnPropertyChanged(nameof(Y)); }
    }

    public int ExtraWaitMs
    {
        get => _extraWaitMs;
        set { _extraWaitMs = value; OnPropertyChanged(nameof(ExtraWaitMs)); }
    }

    public ClickPoint() { }

    public ClickPoint(int x, int y, int extraWaitMs = 0)
    {
        _x = x;
        _y = y;
        _extraWaitMs = extraWaitMs;
    }

    public ClickPoint Clone() => new(X, Y, ExtraWaitMs);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
