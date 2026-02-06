namespace ZeitManager.Models;

public class MathChallenge
{
    private static readonly Random _random = new();
    private static readonly object _lock = new();

    public string Question { get; set; } = string.Empty;
    public int Answer { get; set; }

    public static MathChallenge Generate(ChallengeDifficulty difficulty)
    {
        lock (_lock)
        {
            return difficulty switch
            {
                ChallengeDifficulty.Easy => GenerateEasy(_random),
                ChallengeDifficulty.Medium => GenerateMedium(_random),
                ChallengeDifficulty.Hard => GenerateHard(_random),
                _ => GenerateEasy(_random)
            };
        }
    }

    private static MathChallenge GenerateEasy(Random random)
    {
        var operation = random.Next(0, 2);
        if (operation == 0)
        {
            var a = random.Next(1, 21);
            var b = random.Next(1, 21);
            return new MathChallenge { Question = $"{a} + {b} = ?", Answer = a + b };
        }
        else
        {
            var a = random.Next(10, 31);
            var b = random.Next(1, Math.Min(21, a));
            return new MathChallenge { Question = $"{a} - {b} = ?", Answer = a - b };
        }
    }

    private static MathChallenge GenerateMedium(Random random)
    {
        var operation = random.Next(0, 3);
        if (operation == 0)
        {
            var a = random.Next(2, 21);
            var b = random.Next(2, 13);
            return new MathChallenge { Question = $"{a} \u00d7 {b} = ?", Answer = a * b };
        }
        else if (operation == 1)
        {
            var a = random.Next(20, 100);
            var b = random.Next(10, 50);
            var isAdd = random.Next(0, 2) == 0;
            return isAdd
                ? new MathChallenge { Question = $"{a} + {b} = ?", Answer = a + b }
                : new MathChallenge { Question = $"{a} - {b} = ?", Answer = a - b };
        }
        else
        {
            var divisor = random.Next(2, 13);
            var quotient = random.Next(2, 21);
            var dividend = divisor * quotient;
            return new MathChallenge { Question = $"{dividend} \u00f7 {divisor} = ?", Answer = quotient };
        }
    }

    private static MathChallenge GenerateHard(Random random)
    {
        var operation = random.Next(0, 3);
        if (operation == 0)
        {
            var a = random.Next(100, 500);
            var b = random.Next(50, 300);
            var c = random.Next(20, 150);
            var pattern = random.Next(0, 2);
            return pattern == 0
                ? new MathChallenge { Question = $"{a} + {b} - {c} = ?", Answer = a + b - c }
                : new MathChallenge { Question = $"{a} - {b} + {c} = ?", Answer = a - b + c };
        }
        else if (operation == 1)
        {
            var a = random.Next(15, 51);
            var b = random.Next(12, 31);
            return new MathChallenge { Question = $"{a} \u00d7 {b} = ?", Answer = a * b };
        }
        else
        {
            var a = random.Next(5, 21);
            var b = random.Next(2, 11);
            var c = random.Next(10, 51);
            return new MathChallenge { Question = $"{a} \u00d7 {b} + {c} = ?", Answer = a * b + c };
        }
    }
}
