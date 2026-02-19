using HandwerkerImperium.Models;

namespace HandwerkerImperium.Services.Interfaces;

/// <summary>
/// Lokale Push-Benachrichtigungen (Android: AlarmManager + BroadcastReceiver).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Plant Benachrichtigungen basierend auf dem aktuellen Spielstand.
    /// </summary>
    void ScheduleGameNotifications(GameState state);

    /// <summary>
    /// Alle geplanten Benachrichtigungen abbrechen.
    /// </summary>
    void CancelAllNotifications();
}
