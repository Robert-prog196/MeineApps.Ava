using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MeineApps.Core.Ava.Localization;
using SQLite;

namespace ZeitManager.Models;

[Table("Alarms")]
public partial class AlarmItem : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public long TimeTicks { get; set; }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int RepeatDays { get; set; } = (int)WeekDays.None;
    public string AlarmTone { get; set; } = "default";
    public bool Vibrate { get; set; } = true;
    public int SnoozeDurationMinutes { get; set; } = 5;
    public int MaxSnoozeCount { get; set; } = 3;
    public int CurrentSnoozeCount { get; set; }
    public bool IsShiftAlarm { get; set; }
    public int? ShiftScheduleId { get; set; }
    public string? NextTrigger { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public bool ChallengeEnabled { get; set; }
    public int ChallengeTypeValue { get; set; } = (int)Models.ChallengeType.None;
    public int ChallengeDifficultyValue { get; set; } = (int)ChallengeDifficulty.Easy;
    public int ShakeCount { get; set; } = 20;

    // Computed properties (not in DB)

    [Ignore]
    public TimeOnly Time
    {
        get => new(TimeTicks);
        set => TimeTicks = value.Ticks;
    }

    [Ignore]
    public WeekDays RepeatDaysEnum
    {
        get => (WeekDays)RepeatDays;
        set => RepeatDays = (int)value;
    }

    [Ignore]
    public DateTime? NextTriggerDateTime
    {
        get => string.IsNullOrEmpty(NextTrigger) ? null : DateTime.Parse(NextTrigger, null, DateTimeStyles.RoundtripKind);
        set => NextTrigger = value?.ToString("O");
    }

    [Ignore]
    public bool IsRepeating => RepeatDays != (int)WeekDays.None;

    [Ignore]
    public string TimeFormatted => Time.ToString("HH:mm");

    [Ignore]
    public ChallengeType ChallengeType
    {
        get => (ChallengeType)ChallengeTypeValue;
        set => ChallengeTypeValue = (int)value;
    }

    [Ignore]
    public ChallengeDifficulty ChallengeDifficulty
    {
        get => (ChallengeDifficulty)ChallengeDifficultyValue;
        set => ChallengeDifficultyValue = (int)value;
    }

    public void NotifyLocalizationChanged()
    {
        OnPropertyChanged(nameof(RepeatDaysFormatted));
    }

    [Ignore]
    public string RepeatDaysFormatted
    {
        get
        {
            var days = RepeatDaysEnum;
            if (days == WeekDays.None) return string.Empty;
            if (days == WeekDays.EveryDay) return LocalizationManager.GetString("Daily");
            if (days == WeekDays.Weekdays) return LocalizationManager.GetString("Weekdays");
            if (days == WeekDays.Weekend) return LocalizationManager.GetString("Weekend");

            var parts = new List<string>();
            if (days.HasFlag(WeekDays.Monday)) parts.Add(LocalizationManager.GetString("MondayShort"));
            if (days.HasFlag(WeekDays.Tuesday)) parts.Add(LocalizationManager.GetString("TuesdayShort"));
            if (days.HasFlag(WeekDays.Wednesday)) parts.Add(LocalizationManager.GetString("WednesdayShort"));
            if (days.HasFlag(WeekDays.Thursday)) parts.Add(LocalizationManager.GetString("ThursdayShort"));
            if (days.HasFlag(WeekDays.Friday)) parts.Add(LocalizationManager.GetString("FridayShort"));
            if (days.HasFlag(WeekDays.Saturday)) parts.Add(LocalizationManager.GetString("SaturdayShort"));
            if (days.HasFlag(WeekDays.Sunday)) parts.Add(LocalizationManager.GetString("SundayShort"));
            return string.Join(", ", parts);
        }
    }
}
