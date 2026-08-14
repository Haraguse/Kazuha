namespace Luminalium.App.Services;

/// <summary>
/// Injectable sound-cue surface for the timer plugin. Live audio delivery is
/// environment-dependent and lands with the Task 22 desktop harness; the
/// default implementation is a safe no-op so the timer never throws.
/// </summary>
public interface IAudioCueService
{
    void PlayFinishedCue();
}

public sealed class NullAudioCueService : IAudioCueService
{
    public static NullAudioCueService Instance { get; } = new();

    private NullAudioCueService()
    {
    }

    public void PlayFinishedCue()
    {
    }
}
