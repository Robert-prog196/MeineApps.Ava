using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using ZeitManager.Services;

namespace ZeitManager.Android.Services;

public class AndroidNotificationService : INotificationService
{
    private const string ChannelId = "zeitmanager_timer";
    private const string AlarmChannelId = "zeitmanager_alarm";

    public AndroidNotificationService()
    {
        CreateNotificationChannels();
    }

    private static void CreateNotificationChannels()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var context = Application.Context;
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager == null) return;

        var timerChannel = new NotificationChannel(ChannelId, "Timer", NotificationImportance.Low)
        {
            Description = "Timer notifications"
        };
        manager.CreateNotificationChannel(timerChannel);

        var alarmChannel = new NotificationChannel(AlarmChannelId, "Alarm", NotificationImportance.High)
        {
            Description = "Alarm notifications"
        };
        alarmChannel.SetSound(null, null); // App handles audio via AudioService
        manager.CreateNotificationChannel(alarmChannel);
    }

    public Task ShowNotificationAsync(string title, string body, string? actionId = null)
    {
        var context = Application.Context;

        if (!NotificationManagerCompat.From(context).AreNotificationsEnabled())
            return Task.CompletedTask;

        var builder = new NotificationCompat.Builder(context, AlarmChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .SetCategory(NotificationCompat.CategoryAlarm);

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(Math.Abs(actionId?.GetHashCode() ?? 1), builder.Build());

        return Task.CompletedTask;
    }

    public Task ScheduleNotificationAsync(string id, string title, string body, DateTime triggerAt)
    {
        var context = Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null) return Task.CompletedTask;

        // Check exact alarm permission on Android 12+
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !alarmManager.CanScheduleExactAlarms())
            return Task.CompletedTask;

        var intent = new Intent(context, typeof(AlarmReceiver));
        intent.PutExtra("title", title);
        intent.PutExtra("body", body);
        intent.PutExtra("id", id);

        var pendingIntent = PendingIntent.GetBroadcast(
            context, Math.Abs(id.GetHashCode()), intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var triggerMs = (long)(triggerAt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        if (pendingIntent != null)
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pendingIntent);
        }

        return Task.CompletedTask;
    }

    public Task CancelNotificationAsync(string id)
    {
        var context = Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager == null) return Task.CompletedTask;

        var intent = new Intent(context, typeof(AlarmReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(
            context, Math.Abs(id.GetHashCode()), intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        if (pendingIntent != null)
        {
            alarmManager.Cancel(pendingIntent);
        }

        var manager = NotificationManagerCompat.From(context);
        manager.Cancel(Math.Abs(id.GetHashCode()));

        return Task.CompletedTask;
    }
}
