namespace FinanzRechner.Services;

public class NotificationService : INotificationService
{
    public Task SendBudgetAlertAsync(string categoryName, double percentageUsed, double spent, double limit)
    {
        // Desktop: no-op for now, can be extended with platform-specific notifications
        return Task.CompletedTask;
    }

    public Task<bool> AreNotificationsAllowedAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> RequestNotificationPermissionAsync()
    {
        return Task.FromResult(true);
    }
}
