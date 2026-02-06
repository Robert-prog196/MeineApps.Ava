using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace MeineApps.UI.Controls;

public partial class WheelPicker : UserControl
{
    public static readonly StyledProperty<int> ValueProperty =
        AvaloniaProperty.Register<WheelPicker, int>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceValue);

    public static readonly StyledProperty<int> MinimumProperty =
        AvaloniaProperty.Register<WheelPicker, int>(nameof(Minimum), 0);

    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<WheelPicker, int>(nameof(Maximum), 59);

    public static readonly StyledProperty<string> FormatStringProperty =
        AvaloniaProperty.Register<WheelPicker, string>(nameof(FormatString), "D2");

    private const double ItemHeight = 40;
    private TextBlock[] _labels = [];
    private bool _isDragging;
    private Point _lastPoint;
    private double _dragAccum;

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    public WheelPicker()
    {
        InitializeComponent();
        _labels = [Item0, Item1, Item2, Item3, Item4];
        UpdateLabels();
    }

    private static int CoerceValue(AvaloniaObject obj, int value)
    {
        var picker = (WheelPicker)obj;
        int range = picker.Maximum - picker.Minimum + 1;
        if (range <= 0) return picker.Minimum;
        while (value < picker.Minimum) value += range;
        while (value > picker.Maximum) value -= range;
        return value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty ||
            change.Property == FormatStringProperty)
        {
            // Re-coerce value when bounds change
            if (change.Property == MinimumProperty || change.Property == MaximumProperty)
            {
                CoerceValue(ValueProperty);
            }
            UpdateLabels();
        }
    }

    private void UpdateLabels()
    {
        if (_labels.Length == 0) return;

        int range = Maximum - Minimum + 1;
        if (range <= 0) return;

        int[] offsets = [-2, -1, 0, 1, 2];
        for (int i = 0; i < _labels.Length; i++)
        {
            int val = Value + offsets[i];
            while (val < Minimum) val += range;
            while (val > Maximum) val -= range;
            _labels[i].Text = val.ToString(FormatString);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _isDragging = true;
        _lastPoint = e.GetPosition(this);
        _dragAccum = 0;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging) return;

        var pos = e.GetPosition(this);
        double deltaY = pos.Y - _lastPoint.Y;
        _lastPoint = pos;
        _dragAccum += deltaY;

        // Drag up (negative delta) = increment, drag down = decrement
        while (_dragAccum <= -ItemHeight * 0.6)
        {
            Value++;
            _dragAccum += ItemHeight;
        }
        while (_dragAccum >= ItemHeight * 0.6)
        {
            Value--;
            _dragAccum -= ItemHeight;
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        _dragAccum = 0;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y > 0) Value++;
        else if (e.Delta.Y < 0) Value--;
        e.Handled = true;
    }
}
