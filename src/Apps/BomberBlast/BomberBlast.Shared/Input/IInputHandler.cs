using BomberBlast.Models.Entities;

namespace BomberBlast.Input;

/// <summary>
/// Interface for input handlers (joystick, swipe, d-pad)
/// </summary>
public interface IInputHandler
{
    /// <summary>Name of this input method for settings</summary>
    string Name { get; }

    /// <summary>Current movement direction</summary>
    Direction MovementDirection { get; }

    /// <summary>Whether bomb button was just pressed (consume once)</summary>
    bool BombPressed { get; }

    /// <summary>Whether detonator button was pressed (for manual detonation)</summary>
    bool DetonatePressed { get; }

    /// <summary>Whether handler is currently receiving input</summary>
    bool IsActive { get; }

    /// <summary>
    /// Handle touch start event
    /// </summary>
    void OnTouchStart(float x, float y, float screenWidth, float screenHeight, long pointerId = 0);

    /// <summary>
    /// Handle touch move event
    /// </summary>
    void OnTouchMove(float x, float y, long pointerId = 0);

    /// <summary>
    /// Handle touch end event
    /// </summary>
    void OnTouchEnd(long pointerId = 0);

    /// <summary>
    /// Update input state (call every frame)
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Reset input state
    /// </summary>
    void Reset();

    /// <summary>
    /// Render input UI (if any)
    /// </summary>
    void Render(SkiaSharp.SKCanvas canvas, float screenWidth, float screenHeight);
}
