namespace BomberBlast.Input;

/// <summary>
/// Available input methods
/// </summary>
public enum InputType
{
    /// <summary>Floating joystick that appears where you touch</summary>
    FloatingJoystick,

    /// <summary>Swipe gestures for movement</summary>
    SwipeGesture,

    /// <summary>Classic fixed D-Pad</summary>
    ClassicDPad,

    /// <summary>Keyboard input (Arrow keys/WASD + Space)</summary>
    Keyboard
}
