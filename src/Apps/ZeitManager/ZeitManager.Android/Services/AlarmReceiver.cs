using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace ZeitManager.Android.Services;

[BroadcastReceiver(Exported = false)]
public class AlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        var title = intent.GetStringExtra("title") ?? "ZeitManager";
        var body = intent.GetStringExtra("body") ?? "Alarm!";
        var id = intent.GetStringExtra("id") ?? "alarm";

        // Check notification permission
        if (!NotificationManagerCompat.From(context).AreNotificationsEnabled())
            return;

        // Create an intent to open the app when notification is tapped
        var tapIntent = new Intent(context, typeof(MainActivity));
        tapIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        var pendingTapIntent = PendingIntent.GetActivity(
            context, Math.Abs(id.GetHashCode()), tapIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(context, "zeitmanager_alarm")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .SetCategory(NotificationCompat.CategoryAlarm);

        // FullScreenIntent only works reliably on API < 31
        if (Build.VERSION.SdkInt < BuildVersionCodes.S)
        {
            builder.SetFullScreenIntent(pendingTapIntent, true);
        }
        else
        {
            builder.SetContentIntent(pendingTapIntent);
        }

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(Math.Abs(id.GetHashCode()), builder.Build());
    }
}
