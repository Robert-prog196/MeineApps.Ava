using BomberBlast.Models.Entities;
using SkiaSharp;

namespace BomberBlast.Input;

/// <summary>
/// Klassischer fester D-Pad Input-Handler
/// </summary>
public class DPadHandler : IInputHandler, IDisposable
{
    public string Name => "Classic D-Pad";

    // D-Pad Konfiguration
    private float _dpadSize = 150f;
    private float _buttonSize = 45f;
    private float _dpadX, _dpadY;
    private float _bombButtonX, _bombButtonY;
    private float _bombButtonRadius = 50f;
    private float _opacity = 0.8f;

    // Gecachte SKFont/SKPaint (einmalig erstellt, vermeidet per-Frame Allokationen)
    private readonly SKFont _arrowFont = new() { Size = 24 };
    private readonly SKPaint _arrowTextPaint = new() { IsAntialias = true };

    // Touch state
    private Direction _currentDirection = Direction.None;
    private bool _bombPressed;
    private bool _bombConsumed;
    private bool _bombButtonTouched;

    // Which button is pressed
    private Direction? _pressedButton;

    public Direction MovementDirection => _currentDirection;
    public bool BombPressed => _bombPressed && !_bombConsumed;
    public bool DetonatePressed => false;
    public bool IsActive => _pressedButton.HasValue || _bombButtonTouched;

    public float DPadSize
    {
        get => _dpadSize;
        set => _dpadSize = value;
    }

    public float Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.1f, 1f);
    }

    public void OnTouchStart(float x, float y, float screenWidth, float screenHeight)
    {
        UpdatePositions(screenWidth, screenHeight);

        // Check bomb button
        float dx = x - _bombButtonX;
        float dy = y - _bombButtonY;
        if (dx * dx + dy * dy <= _bombButtonRadius * _bombButtonRadius * 1.3f)
        {
            _bombButtonTouched = true;
            _bombPressed = true;
            _bombConsumed = false;
            return;
        }

        // Check D-Pad buttons
        CheckDPadPress(x, y);
    }

    public void OnTouchMove(float x, float y)
    {
        if (_bombButtonTouched)
            return;

        // Update D-Pad press based on current touch position
        CheckDPadPress(x, y);
    }

    public void OnTouchEnd()
    {
        _pressedButton = null;
        _currentDirection = Direction.None;
        _bombButtonTouched = false;
    }

    public void Update(float deltaTime)
    {
        // Consume bomb press
        if (_bombConsumed)
        {
            _bombPressed = false;
        }
        if (_bombPressed)
        {
            _bombConsumed = true;
        }

        // Update current direction from pressed button
        _currentDirection = _pressedButton ?? Direction.None;
    }

    public void Reset()
    {
        _pressedButton = null;
        _currentDirection = Direction.None;
        _bombPressed = false;
        _bombConsumed = false;
        _bombButtonTouched = false;
    }

    private void UpdatePositions(float screenWidth, float screenHeight)
    {
        // D-Pad in bottom left (less margin for landscape)
        _dpadX = 30 + _dpadSize / 2;
        _dpadY = screenHeight - 20 - _dpadSize / 2;

        // Bomb button in bottom right (less margin for landscape)
        _bombButtonX = screenWidth - _bombButtonRadius - 30;
        _bombButtonY = screenHeight - _bombButtonRadius - 20;
    }

    private void CheckDPadPress(float x, float y)
    {
        float centerX = _dpadX;
        float centerY = _dpadY;
        float buttonDist = _dpadSize / 2 - _buttonSize / 2;

        // Check each direction button
        var buttons = new[]
        {
            (Direction.Up, centerX, centerY - buttonDist),
            (Direction.Down, centerX, centerY + buttonDist),
            (Direction.Left, centerX - buttonDist, centerY),
            (Direction.Right, centerX + buttonDist, centerY)
        };

        _pressedButton = null;

        foreach (var (dir, bx, by) in buttons)
        {
            float dx = x - bx;
            float dy = y - by;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist <= _buttonSize)
            {
                _pressedButton = dir;
                break;
            }
        }
    }

    public void Render(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        UpdatePositions(screenWidth, screenHeight);
        byte alpha = (byte)(_opacity * 255);

        float centerX = _dpadX;
        float centerY = _dpadY;
        float buttonDist = _dpadSize / 2 - _buttonSize / 2;

        // Draw D-Pad background
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(50, 50, 50, (byte)(alpha * 0.5f)),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        // Kreuz-Form Hintergrund
        using var crossPath = new SKPath();
        float cs = _buttonSize; // Cross segment size
        float cd = buttonDist; // Cross distance from center

        // Vertical part
        crossPath.AddRect(new SKRect(centerX - cs, centerY - cd - cs, centerX + cs, centerY + cd + cs));
        // Horizontal part
        crossPath.AddRect(new SKRect(centerX - cd - cs, centerY - cs, centerX + cd + cs, centerY + cs));

        canvas.DrawPath(crossPath, bgPaint);

        // Draw directional buttons
        var buttons = new[]
        {
            (Direction.Up, centerX, centerY - buttonDist, "\u25B2"),
            (Direction.Down, centerX, centerY + buttonDist, "\u25BC"),
            (Direction.Left, centerX - buttonDist, centerY, "\u25C0"),
            (Direction.Right, centerX + buttonDist, centerY, "\u25B6")
        };

        foreach (var (dir, bx, by, symbol) in buttons)
        {
            bool isPressed = _pressedButton == dir;

            using var buttonPaint = new SKPaint
            {
                Color = isPressed
                    ? new SKColor(150, 150, 150, alpha)
                    : new SKColor(100, 100, 100, alpha),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawCircle(bx, by, _buttonSize, buttonPaint);

            using var borderPaint = new SKPaint
            {
                Color = new SKColor(200, 200, 200, alpha),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true
            };
            canvas.DrawCircle(bx, by, _buttonSize, borderPaint);

            // Pfeil-Symbol (gecachte Font/Paint wiederverwenden)
            _arrowTextPaint.Color = new SKColor(255, 255, 255, alpha);
            canvas.DrawText(symbol, bx, by + 8, SKTextAlign.Center, _arrowFont, _arrowTextPaint);
        }

        // Draw bomb button
        using var bombBgPaint = new SKPaint
        {
            Color = _bombButtonTouched
                ? new SKColor(255, 100, 100, alpha)
                : new SKColor(255, 50, 50, alpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(_bombButtonX, _bombButtonY, _bombButtonRadius, bombBgPaint);

        // Bomb icon
        using var bombPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, alpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        float bombSize = _bombButtonRadius * 0.5f;
        canvas.DrawCircle(_bombButtonX, _bombButtonY + bombSize * 0.1f, bombSize, bombPaint);

        // Fuse
        using var fusePaint = new SKPaint
        {
            Color = new SKColor(255, 200, 0, alpha),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        using var fusePath = new SKPath();
        fusePath.MoveTo(_bombButtonX, _bombButtonY - bombSize);
        fusePath.QuadTo(
            _bombButtonX + bombSize * 0.3f, _bombButtonY - bombSize - 10,
            _bombButtonX + bombSize * 0.5f, _bombButtonY - bombSize - 5);
        canvas.DrawPath(fusePath, fusePaint);
    }

    public void Dispose()
    {
        _arrowFont.Dispose();
        _arrowTextPaint.Dispose();
    }
}
