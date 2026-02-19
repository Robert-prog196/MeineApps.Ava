using HandwerkerImperium.Models;
using HandwerkerImperium.Services.Interfaces;

namespace HandwerkerImperium.Services;

/// <summary>
/// Desktop-Stub für Benachrichtigungen (keine native Unterstützung).
/// </summary>
public class NotificationService : INotificationService
{
    public void ScheduleGameNotifications(GameState state) { }
    public void CancelAllNotifications() { }
}
