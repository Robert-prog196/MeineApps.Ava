using MeineApps.Core.Ava.Localization;
using SQLite;

namespace ZeitManager.Models;

[Table("CustomPatterns")]
public class CustomShiftPattern
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated ShiftType values, e.g. "1,1,1,2,2,3,3,0,0,0"
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    public int GroupCount { get; set; } = 1;

    public int GroupOffset { get; set; }

    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    // Computed properties

    [Ignore]
    public int CycleLength => PatternArray.Length;

    [Ignore]
    public ShiftType[] PatternArray
    {
        get
        {
            if (string.IsNullOrEmpty(Pattern)) return [];
            return Pattern.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var v) ? (ShiftType)v : ShiftType.Free)
                .ToArray();
        }
        set => Pattern = string.Join(",", value.Select(s => (int)s));
    }

    [Ignore]
    public string PatternSummary
    {
        get
        {
            var shifts = PatternArray;
            if (shifts.Length == 0) return "No pattern";

            var groups = new List<string>();
            var current = shifts[0];
            var count = 1;

            for (int i = 1; i < shifts.Length; i++)
            {
                if (shifts[i] == current)
                {
                    count++;
                }
                else
                {
                    groups.Add($"{count}x{ShortName(current)}");
                    current = shifts[i];
                    count = 1;
                }
            }
            groups.Add($"{count}x{ShortName(current)}");
            return string.Join(", ", groups);
        }
    }

    private static string ShortName(ShiftType shift) => shift switch
    {
        ShiftType.Early => LocalizationManager.GetString("ShiftEarlyShort"),
        ShiftType.Late => LocalizationManager.GetString("ShiftLateShort"),
        ShiftType.Night => LocalizationManager.GetString("ShiftNightShort"),
        ShiftType.Free => "-",
        _ => "?"
    };

    public static CustomShiftPattern Create21ShiftPattern() => new()
    {
        Name = "21-Shift (Standard)",
        PatternArray =
        [
            ShiftType.Early, ShiftType.Early,
            ShiftType.Late, ShiftType.Late,
            ShiftType.Night, ShiftType.Night,
            ShiftType.Free, ShiftType.Free, ShiftType.Free, ShiftType.Free
        ],
        GroupCount = 5,
        GroupOffset = 2
    };
}
