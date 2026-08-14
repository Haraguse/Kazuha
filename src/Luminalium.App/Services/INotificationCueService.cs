namespace Luminalium.App.Services;

/// <summary>
/// Injectable notification surface for the timer plugin. Real tray/toast
/// delivery is environment-dependent and lands with the Task 22 desktop
/// harness; the default implementation is a safe no-op.
/// </summary>
public interface INotificationCueService
{
    void ShowFinished(string? title, string? body);
}

public sealed class NullNotificationCueService : INotificationCueService
{
    public static NullNotificationCueService Instance { get; } = new();

    private NullNotificationCueService()
    {
    }

    public void ShowFinished(string? title, string? body)
    {
    }
}
