namespace FinanzRechner.Services;

/// <summary>
/// Service for local notifications
/// </summary>
public interface INotificationService
{
    Task SendBudgetAlertAsync(string categoryName, double percentageUsed, double spent, double limit);
    Task<bool> AreNotificationsAllowedAsync();
    Task<bool> RequestNotificationPermissionAsync();
}
