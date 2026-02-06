namespace ZeitManager.Models;

public record StopwatchLap(
    int LapNumber,
    TimeSpan LapTime,
    TimeSpan TotalTime,
    DateTime Timestamp)
{
    public string LapTimeFormatted => FormatTime(LapTime);
    public string TotalTimeFormatted => FormatTime(TotalTime);

    private static string FormatTime(TimeSpan time)
    {
        if (time.Hours > 0)
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        return $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
    }
}
