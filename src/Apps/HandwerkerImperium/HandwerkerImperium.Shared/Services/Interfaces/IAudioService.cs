namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Handles audio playback for sound effects and music.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Whether sound effects are enabled.
    /// </summary>
    bool SoundEnabled { get; set; }

    /// <summary>
    /// Whether music is enabled.
    /// </summary>
    bool MusicEnabled { get; set; }

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    Task PlaySoundAsync(GameSound sound);

    /// <summary>
    /// Plays background music (loops).
    /// </summary>
    Task PlayMusicAsync(string musicFile);

    /// <summary>
    /// Stops the background music.
    /// </summary>
    void StopMusic();

    /// <summary>
    /// Triggers haptic feedback.
    /// </summary>
    void Vibrate(VibrationType type);
}

/// <summary>
/// Sound effect types in the game.
/// </summary>
public enum GameSound
{
    /// <summary>Button tap</summary>
    ButtonTap,

    /// <summary>Money earned (cha-ching!)</summary>
    MoneyEarned,

    /// <summary>Level up fanfare</summary>
    LevelUp,

    /// <summary>Workshop upgraded</summary>
    Upgrade,

    /// <summary>Worker hired</summary>
    WorkerHired,

    /// <summary>Mini-game perfect rating</summary>
    Perfect,

    /// <summary>Mini-game good rating</summary>
    Good,

    /// <summary>Mini-game miss</summary>
    Miss,

    /// <summary>Order completed</summary>
    OrderComplete,

    /// <summary>Sawing sound</summary>
    Sawing,

    /// <summary>Hammering sound</summary>
    Hammering,

    /// <summary>Drilling sound</summary>
    Drilling
}

/// <summary>
/// Haptic feedback types.
/// </summary>
public enum VibrationType
{
    /// <summary>Light tap</summary>
    Light,

    /// <summary>Medium impact</summary>
    Medium,

    /// <summary>Heavy impact</summary>
    Heavy,

    /// <summary>Success pattern</summary>
    Success,

    /// <summary>Error pattern</summary>
    Error
}
