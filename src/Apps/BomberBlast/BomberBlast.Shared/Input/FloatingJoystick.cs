using BomberBlast.Models.Entities;
using SkiaSharp;

namespace BomberBlast.Input;

/// <summary>
/// Floating joystick that appears where the player touches
/// </summary>
public class FloatingJoystick : IInputHandler
{
    public string Name => "Floating Joystick";

    // Joystick state
    private bool _isPressed;
    private float _baseX, _baseY;      // Center of joystick
    private float _stickX, _stickY;    // Current stick position
    // Bomb button state
    private bool _bombPressed;
    private bool _bombConsumed;
    private float _bombButtonX, _bombButtonY;
    private bool _bombButtonPressed;

    // Configuration
    private float _joystickRadius = 60f;
    private float _deadZone = 0.08f;  // Reduced from 0.2 for more responsive controls
    private float _bombButtonRadius = 50f;
    private float _opacity = 0.7f;

    // Movement
    private Direction _currentDirection = Direction.None;

    public Direction MovementDirection => _currentDirection;
    public bool BombPressed => _bombPressed && !_bombConsumed;
    public bool DetonatePressed => false;
    public bool IsActive => _isPressed;

    public float JoystickSize
    {
        get => _joystickRadius * 2;
        set => _joystickRadius = value / 2;
    }

    public float Opacity
    {
        get => _opacity;
        set => _opacity = Math.Clamp(value, 0.1f, 1f);
    }

    public void OnTouchStart(float x, float y, float screenWidth, float screenHeight)
    {
        // Check if touching bomb button (right side of screen)
        UpdateBombButtonPosition(screenWidth, screenHeight);

        float dx = x - _bombButtonX;
        float dy = y - _bombButtonY;
        if (dx * dx + dy * dy <= _bombButtonRadius * _bombButtonRadius * 1.5f)
        {
            _bombButtonPressed = true;
            _bombPressed = true;
            _bombConsumed = false;
            return;
        }

        // Left half of screen - joystick
        if (x < screenWidth * 0.6f)
        {
            _isPressed = true;
            _baseX = x;
            _baseY = y;
            _stickX = x;
            _stickY = y;
            UpdateDirection();
        }
    }

    public void OnTouchMove(float x, float y)
    {
        if (!_isPressed)
            return;

        _stickX = x;
        _stickY = y;

        // Clamp stick to joystick radius
        float dx = _stickX - _baseX;
        float dy = _stickY - _baseY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        if (distance > _joystickRadius)
        {
            float ratio = _joystickRadius / distance;
            _stickX = _baseX + dx * ratio;
            _stickY = _baseY + dy * ratio;
        }

        UpdateDirection();
    }

    public void OnTouchEnd()
    {
        _isPressed = false;
        _stickX = _baseX;
        _stickY = _baseY;
        _currentDirection = Direction.None;
        _bombButtonPressed = false;
    }

    public void Update(float deltaTime)
    {
        // Consume bomb press after frame
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
        _isPressed = false;
        _currentDirection = Direction.None;
        _bombPressed = false;
        _bombConsumed = false;
        _bombButtonPressed = false;
    }

    private void UpdateDirection()
    {
        float dx = _stickX - _baseX;
        float dy = _stickY - _baseY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);

        // Apply dead zone
        if (distance < _joystickRadius * _deadZone)
        {
            _currentDirection = Direction.None;
            return;
        }

        // Determine primary direction (4-way, not 8-way)
        float angle = MathF.Atan2(dy, dx);

        // Convert angle to direction (-PI to PI)
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

    private void UpdateBombButtonPosition(float screenWidth, float screenHeight)
    {
        _bombButtonX = screenWidth - _bombButtonRadius - 30;
        _bombButtonY = screenHeight - _bombButtonRadius - 20;
    }

    public void Render(SKCanvas canvas, float screenWidth, float screenHeight)
    {
        UpdateBombButtonPosition(screenWidth, screenHeight);
        byte alpha = (byte)(_opacity * 255);

        // Draw joystick if pressed
        if (_isPressed)
        {
            // Base circle
            using var basePaint = new SKPaint
            {
                Color = new SKColor(100, 100, 100, (byte)(alpha * 0.5f)),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(_baseX, _baseY, _joystickRadius, basePaint);

            // Base border
            using var borderPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, alpha),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3,
                IsAntialias = true
            };
            canvas.DrawCircle(_baseX, _baseY, _joystickRadius, borderPaint);

            // Stick
            using var stickPaint = new SKPaint
            {
                Color = new SKColor(255, 255, 255, alpha),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawCircle(_stickX, _stickY, _joystickRadius * 0.4f, stickPaint);
        }

        // Draw bomb button
        using var bombBgPaint = new SKPaint
        {
            Color = _bombButtonPressed
                ? new SKColor(255, 100, 100, alpha)
                : new SKColor(255, 50, 50, alpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(_bombButtonX, _bombButtonY, _bombButtonRadius, bombBgPaint);

        // Bomb icon (simple bomb shape)
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

        // Spark
        canvas.DrawCircle(_bombButtonX + bombSize * 0.5f, _bombButtonY - bombSize - 5, 4, bombPaint);
    }
}
