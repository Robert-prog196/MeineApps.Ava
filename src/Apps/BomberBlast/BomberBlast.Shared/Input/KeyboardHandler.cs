using Avalonia.Input;
using BomberBlast.Models.Entities;
using SkiaSharp;

namespace BomberBlast.Input;

/// <summary>
/// Keyboard input handler for desktop platforms.
/// Supports Arrow keys and WASD for movement, Space for bomb, E for detonation.
/// </summary>
public class KeyboardHandler : IInputHandler
{
    public string Name => "Keyboard";

    private readonly HashSet<Key> _pressedKeys = new();
    private bool _bombPressed;
    private bool _bombConsumed;
    private bool _detonatePressed;
    private bool _detonateConsumed;

    public Direction MovementDirection
    {
        get
        {
            if (_pressedKeys.Contains(Key.Up) || _pressedKeys.Contains(Key.W))
                return Direction.Up;
            if (_pressedKeys.Contains(Key.Down) || _pressedKeys.Contains(Key.S))
                return Direction.Down;
            if (_pressedKeys.Contains(Key.Left) || _pressedKeys.Contains(Key.A))
                return Direction.Left;
            if (_pressedKeys.Contains(Key.Right) || _pressedKeys.Contains(Key.D))
                return Direction.Right;
            return Direction.None;
        }
    }

    public bool BombPressed => _bombPressed && !_bombConsumed;
    public bool DetonatePressed => _detonatePressed && !_detonateConsumed;
    public bool IsActive => _pressedKeys.Count > 0;

    public void OnKeyDown(Key key)
    {
        _pressedKeys.Add(key);

        if (key == Key.Space)
        {
            _bombPressed = true;
            _bombConsumed = false;
        }

        if (key == Key.E)
        {
            _detonatePressed = true;
            _detonateConsumed = false;
        }
    }

    public void OnKeyUp(Key key)
    {
        _pressedKeys.Remove(key);
    }

    // Touch methods are no-ops for keyboard handler
    public void OnTouchStart(float x, float y, float screenWidth, float screenHeight) { }
    public void OnTouchMove(float x, float y) { }
    public void OnTouchEnd() { }

    public void Update(float deltaTime)
    {
        if (_bombConsumed)
            _bombPressed = false;
        if (_bombPressed)
            _bombConsumed = true;

        if (_detonateConsumed)
            _detonatePressed = false;
        if (_detonatePressed)
            _detonateConsumed = true;
    }

    public void Reset()
    {
        _pressedKeys.Clear();
        _bombPressed = false;
        _bombConsumed = false;
        _detonatePressed = false;
        _detonateConsumed = false;
    }

    // No visual controls needed for keyboard
    public void Render(SKCanvas canvas, float screenWidth, float screenHeight) { }
}
