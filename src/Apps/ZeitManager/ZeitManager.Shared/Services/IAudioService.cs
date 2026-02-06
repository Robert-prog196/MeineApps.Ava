using ZeitManager.Models;

namespace ZeitManager.Services;

public interface IAudioService
{
    IReadOnlyList<SoundItem> AvailableSounds { get; }
    Task PlayAsync(string soundId, bool loop = false);
    void Stop();
    string DefaultTimerSound { get; }
    string DefaultAlarmSound { get; }
}
