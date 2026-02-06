namespace FinanzRechner.Models;

/// <summary>
/// Budget limit for a category
/// </summary>
public class Budget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public ExpenseCategory Category { get; set; }
    public double MonthlyLimit { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Warning threshold in percent (e.g. 80 = warning at 80% of limit)
    /// </summary>
    public double WarningThreshold { get; set; } = 80;
}

/// <summary>
/// Budget status for a category
/// </summary>
public record BudgetStatus(
    ExpenseCategory Category,
    double Limit,
    double Spent,
    double Remaining,
    double PercentageUsed,
    BudgetAlertLevel AlertLevel)
{
    public bool IsExceeded => AlertLevel == BudgetAlertLevel.Exceeded;
    public bool IsWarning => AlertLevel == BudgetAlertLevel.Warning;
    public string CategoryName => Category.ToString();
    public string CategoryIcon => Category switch
    {
        ExpenseCategory.Food => "\U0001F354",
        ExpenseCategory.Transport => "\U0001F697",
        ExpenseCategory.Housing => "\U0001F3E0",
        ExpenseCategory.Entertainment => "\U0001F3AC",
        ExpenseCategory.Shopping => "\U0001F6D2",
        ExpenseCategory.Health => "\U0001F48A",
        ExpenseCategory.Education => "\U0001F4DA",
        ExpenseCategory.Bills => "\U0001F4C4",
        _ => "\U0001F4E6"
    };
    public string SpentDisplay => $"{Spent:N2} \u20ac";
    public string LimitDisplay => $"{Limit:N2} \u20ac";
    public string PercentageDisplay => $"{PercentageUsed:F0}%";
};

/// <summary>
/// Budget alert level
/// </summary>
public enum BudgetAlertLevel
{
    Safe,
    Warning,
    Exceeded
}
