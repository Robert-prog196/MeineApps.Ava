using Android.App;
using Android.Content;

namespace ZeitManager.Android.Services;

[BroadcastReceiver(Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null) return;
        if (intent?.Action != Intent.ActionBootCompleted) return;

        try
        {
            // Launch the app's main activity to re-initialize alarm scheduler
            // App.OnFrameworkInitializationCompleted will call AlarmSchedulerService.InitializeAsync()
            // which reloads all active alarms from the database
            var launchIntent = new Intent(context, typeof(MainActivity));
            launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.NoAnimation);
            context.StartActivity(launchIntent);
        }
        catch
        {
            // Boot receiver has 10s timeout - if Activity can't start, ignore silently
        }
    }
}
