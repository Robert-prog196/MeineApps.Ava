using System.Diagnostics;
using System.Runtime.InteropServices;
using ZeitManager.Models;

namespace ZeitManager.Services;

public class AudioService : IAudioService
{
    private readonly object _lock = new();
    private CancellationTokenSource? _loopCts;

    private static readonly List<SoundItem> _sounds =
    [
        new("default", "Default Beep"),
        new("alert_high", "Alert High"),
        new("alert_low", "Alert Low"),
        new("chime", "Chime"),
        new("bell", "Bell"),
        new("digital", "Digital"),
    ];

    public IReadOnlyList<SoundItem> AvailableSounds => _sounds;
    public string DefaultTimerSound => "default";
    public string DefaultAlarmSound => "default";

    public async Task PlayAsync(string soundId, bool loop = false)
    {
        Stop();

        if (loop)
        {
            CancellationTokenSource cts;
            lock (_lock)
            {
                cts = new CancellationTokenSource();
                _loopCts = cts;
            }
            var token = cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        PlayTone(soundId);
                        await Task.Delay(2000, token);
                    }
                }
                catch (OperationCanceledException) { }
            }, token);
        }
        else
        {
            await Task.Run(() => PlayTone(soundId));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? oldCts;
        lock (_lock)
        {
            oldCts = _loopCts;
            _loopCts = null;
        }

        if (oldCts != null)
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        if (OperatingSystem.IsWindows())
        {
            // Stop any currently playing sound
            PlaySound(null, IntPtr.Zero, 0);
        }
    }

    private static void PlayTone(string soundId)
    {
        try
        {
            var (frequency, durationMs) = soundId switch
            {
                "alert_high" => (1200, 300),
                "alert_low" => (600, 500),
                "chime" => (880, 200),
                "bell" => (1000, 400),
                "digital" => (1500, 150),
                _ => (800, 300) // default
            };

            var wavData = GenerateWav(frequency, durationMs);

            if (OperatingSystem.IsWindows())
            {
                PlaySound(wavData, IntPtr.Zero, SND_MEMORY | SND_SYNC | SND_NODEFAULT);
            }
            else if (OperatingSystem.IsLinux())
            {
                PlaySoundLinux(wavData);
            }
        }
        catch
        {
            // Ignore audio errors silently
        }
    }

    /// <summary>
    /// Generates a PCM WAV file in memory with a sine wave tone.
    /// </summary>
    private static byte[] GenerateWav(int frequency, int durationMs)
    {
        const int sampleRate = 44100;
        int samples = sampleRate * durationMs / 1000;
        int dataSize = samples * 2; // 16-bit mono
        int fadeOut = Math.Min(samples / 10, sampleRate / 20); // ~50ms fade

        using var ms = new MemoryStream(44 + dataSize);
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write((byte)'R'); bw.Write((byte)'I'); bw.Write((byte)'F'); bw.Write((byte)'F');
        bw.Write(36 + dataSize);
        bw.Write((byte)'W'); bw.Write((byte)'A'); bw.Write((byte)'V'); bw.Write((byte)'E');

        // fmt sub-chunk
        bw.Write((byte)'f'); bw.Write((byte)'m'); bw.Write((byte)'t'); bw.Write((byte)' ');
        bw.Write(16);          // chunk size
        bw.Write((short)1);    // PCM format
        bw.Write((short)1);    // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // byte rate
        bw.Write((short)2);    // block align
        bw.Write((short)16);   // bits per sample

        // data sub-chunk
        bw.Write((byte)'d'); bw.Write((byte)'a'); bw.Write((byte)'t'); bw.Write((byte)'a');
        bw.Write(dataSize);

        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / sampleRate;
            double amplitude = 0.5;

            // Fade out at the end to avoid click
            if (i >= samples - fadeOut)
                amplitude *= (double)(samples - i) / fadeOut;

            short sample = (short)(Math.Sin(2 * Math.PI * frequency * t) * short.MaxValue * amplitude);
            bw.Write(sample);
        }

        return ms.ToArray();
    }

    // Windows: PlaySound from winmm.dll (plays through audio card, not PC speaker)
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern bool PlaySound(byte[]? data, IntPtr hmod, uint fdwSound);

    private const uint SND_SYNC = 0x0000;
    private const uint SND_MEMORY = 0x0004;
    private const uint SND_NODEFAULT = 0x0002;

    // Linux: write WAV to temp file and play with aplay/paplay
    private static void PlaySoundLinux(byte[] wavData)
    {
        string[] players = ["paplay", "aplay"];

        foreach (var player in players)
        {
            try
            {
                var tempFile = Path.Combine(Path.GetTempPath(), $"zeitmanager_tone_{Guid.NewGuid():N}.wav");
                File.WriteAllBytes(tempFile, wavData);

                var psi = new ProcessStartInfo
                {
                    FileName = player,
                    Arguments = tempFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    try { File.Delete(tempFile); } catch { }
                    return;
                }
            }
            catch
            {
                continue;
            }
        }
    }
}
