using BomberBlast.Models.Entities;
using SkiaSharp;

namespace BomberBlast.Input;

/// <summary>
/// Swipe-based input handler
/// </summary>
public class SwipeGestureHandler : IInputHandler
{
    public string Name => "Swipe Gestures";

    // Touch state
    private float _startX, _startY;
    private float _currentX, _currentY;
    private bool _isTouching;
    private float _touchTime;

    // Swipe detection
    private const float MIN_SWIPE_DISTANCE = 30f;
    private const float TAP_MAX_TIME = 0.3f;
    private const float TAP_MAX_DISTANCE = 20f;

    // Current input state
    private Direction _currentDirection = Direction.None;
    private bool _bombPressed;
    private bool _bombConsumed;

    public Direction MovementDirection => _currentDirection;
    public bool BombPressed => _bombPressed && !_bombConsumed;
    public bool DetonatePressed => false;
    public bool IsActive => _isTouching;

    public void OnTouchStart(float x, float y, float screenWidth, float screenHeight)
    {
        _startX = x;
        _startY = y;
        _currentX = x;
        _currentY = y;
        _isTouching = true;
        _touchTime = 0;
    }

    public void OnTouchMove(float x, float y)
    {
        if (!_isTouching)
            return;

        _currentX = x;
        _currentY = y;

        // Calculate swipe direction
        float dx = _currentX - _startX;
        float dy = _currentY - _startY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        if (distance >= MIN_SWIPE_DISTANCE)
        {
            // Determine direction from angle
            float angle = MathF.Atan2(dy, dx);

            if (angle >= -MathF.PI / 4 && angle < MathF.PI / 4)
            {
                _currentDirection = Direction.Right;
            }
            else if (angle >= MathF.PI / 4 && angle < 3 * MathF.PI / 4)
            {
                _currentDirection = Direction.Down;
            }
            else if (angle >= -3 * MathF.PI / 4 && angle < -MathF.PI / 4)
            {
                _currentDirection = Direction.Up;
            }
            else
            {
                _currentDirection = Direction.Left;
            }
        }
    }

    public void OnTouchEnd()
    {
        if (!_isTouching)
            return;

        float dx = _currentX - _startX;
        float dy = _currentY - _startY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        // Check for tap (quick touch, small movement = bomb)
        if (_touchTime <= TAP_MAX_TIME && distance <= TAP_MAX_DISTANCE)
        {
            _bombPressed = true;
            _bombConsumed = false;
        }

        _isTouching = false;
        // Keep current direction until next touch
    }

    public void Update(float deltaTime)
    {
        if (_isTouching)
        {
            _touchTime += deltaTime;
        }

        // Consume bomb press
        if (_bombConsumed)
        {
            _bombPressed = false;
        }
        if (_bombPressed)
        {
            _bombConsumed = true;
        }
    }

    public void Reset()
    {
        _isTouching = false;
        _currentDirection = Direction.None;
        _bombPressed = false;
        _bombConsumed = false;
    }

    public void Render(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        // Show swipe indicator if touching
        if (_isTouching)
        {
            float dx = _currentX - _startX;
            float dy = _currentY - _startY;
            float distance = MathF.Sqrt(dx * dx + dy * dy);

            if (distance > 10)
            {
                using var linePaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 128),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 4,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };

                canvas.DrawLine(_startX, _startY, _currentX, _currentY, linePaint);

                // Arrow head
                float angle = MathF.Atan2(dy, dx);
                float arrowSize = 15;
                float arrowAngle = MathF.PI / 6;

                var arrowPath = new SKPath();
                arrowPath.MoveTo(_currentX, _currentY);
                arrowPath.LineTo(
                    _currentX - arrowSize * MathF.Cos(angle - arrowAngle),
                    _currentY - arrowSize * MathF.Sin(angle - arrowAngle));
                arrowPath.MoveTo(_currentX, _currentY);
                arrowPath.LineTo(
                    _currentX - arrowSize * MathF.Cos(angle + arrowAngle),
                    _currentY - arrowSize * MathF.Sin(angle + arrowAngle));

                canvas.DrawPath(arrowPath, linePaint);
            }
        }

        // Show hint text
        using var font = new SKFont { Size = 16 };
        using var textPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 100),
            IsAntialias = true
        };

        // TODO: Use localized strings once resource system is set up
        canvas.DrawText("Swipe to move, Tap to bomb",
            screenWidth / 2, screenHeight - 14, SKTextAlign.Center, font, textPaint);
    }
}
