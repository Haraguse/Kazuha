using System.Text;
using System.Text.Json;
using Windows.Media.Control;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

GlobalSystemMediaTransportControlsSessionManager? manager = null;

while (true)
{
    var requestId = Console.ReadLine();
    if (requestId is null || requestId == "__EXIT__")
    {
        break;
    }

    try
    {
        manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var snapshot = await ReadBestSessionAsync(manager);
        WriteJson(new
        {
            request_id = requestId,
            status = snapshot?.Status ?? "",
            title = snapshot?.Title ?? "",
            artist = snapshot?.Artist ?? "",
            source = snapshot?.Source ?? ""
        });
    }
    catch
    {
        manager = null;
        WriteJson(new
        {
            request_id = requestId,
            status = "Stopped",
            title = "",
            artist = "",
            source = ""
        });
    }
}

static async Task<SessionSnapshot?> ReadBestSessionAsync(GlobalSystemMediaTransportControlsSessionManager manager)
{
    var currentSource = manager.GetCurrentSession()?.SourceAppUserModelId ?? "";
    var sessions = manager.GetSessions();
    var snapshots = new List<SessionSnapshot>();

    foreach (var session in sessions)
    {
        try
        {
            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            snapshots.Add(new SessionSnapshot(
                session.SourceAppUserModelId ?? "",
                playback?.PlaybackStatus.ToString() ?? "",
                props?.Title ?? "",
                props?.Artist ?? "",
                session.SourceAppUserModelId == currentSource
            ));
        }
        catch
        {
            // Ignore individual session failures and keep looking for a usable one.
        }
    }

    return snapshots
        .OrderBy(snapshot => RankStatus(snapshot.Status))
        .ThenByDescending(snapshot => snapshot.IsCurrent)
        .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.Title))
        .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.Artist))
        .FirstOrDefault();
}

static int RankStatus(string status) => status switch
{
    "Playing" => 0,
    "Paused" => 1,
    "Changing" => 2,
    "Stopped" => 3,
    _ => 4,
};

static void WriteJson(object payload)
{
    Console.WriteLine(JsonSerializer.Serialize(payload));
    Console.Out.Flush();
}

internal sealed record SessionSnapshot(
    string Source,
    string Status,
    string Title,
    string Artist,
    bool IsCurrent
);
