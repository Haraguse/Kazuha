using System.Text.Json;

namespace Luminalium.Wps;

public sealed record WpsMessage(
    string MessageType,
    string MessageId,
    long Ts,
    JsonElement Payload);
