using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Luminalium.Core.Platform;
using Luminalium.Wps;
using Xunit;

namespace Luminalium.Tests;

public sealed class WpsBridgeHostTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HappyPathBindsDefaultRangeReceivesHelloAndSendsEnvelope()
    {
        var protocol = new WpsProtocol();
        await using var host = new WpsBridgeHost(protocol);
        var received = new TaskCompletionSource<WpsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OnMessageReceived += message => received.TrySetResult(message);

        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);
        Assert.InRange(started.Value.Port, 3892, 3902);

        using var client = await ConnectAsync(started.Value.Port, "/ws");
        var helloId = protocol.NewMessageId();
        await SendTextAsync(client, protocol.Encode("hello", HelloPayload(), helloId));

        var inbound = await WithTimeout(received.Task);
        Assert.Equal("hello", inbound.MessageType);
        Assert.Equal(helloId, inbound.MessageId);

        var outboundId = protocol.NewMessageId();
        var sent = await host.SendAsync("presentation_next", new { }, outboundId);
        Assert.True(sent.IsSuccess, sent.Error?.Message);
        Assert.Equal(outboundId, sent.Value);

        using var outbound = JsonDocument.Parse(await ReceiveTextAsync(client));
        Assert.Equal("presentation_next", outbound.RootElement.GetProperty("message_type").GetString());
        Assert.Equal(outboundId, outbound.RootElement.GetProperty("message_id").GetString());
        Assert.True(outbound.RootElement.TryGetProperty("ts", out var ts));
        Assert.Equal(JsonValueKind.Number, ts.ValueKind);
        Assert.Equal(JsonValueKind.Object, outbound.RootElement.GetProperty("payload").ValueKind);
    }

    [Fact]
    public async Task StrictModeAcceptsValidTokenAndConnectsClient()
    {
        const string token = "test-token-valid";
        await using var host = new WpsBridgeHost(authenticationToken: token);
        var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OnClientConnected += () => connected.TrySetResult(true);

        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);

        using var client = await ConnectAsync(started.Value.Port, "/ws?token=" + Uri.EscapeDataString(token));
        Assert.True(await WithTimeout(connected.Task));
        Assert.True(host.IsConnected);
        Assert.Equal(WebSocketState.Open, client.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("test-token-invalid")]
    public async Task StrictModeRejectsMissingOrInvalidTokenWithoutReplacingClient(string? token)
    {
        const string expectedToken = "test-token-valid";
        await using var host = new WpsBridgeHost(authenticationToken: expectedToken);
        var connectedCount = 0;
        host.OnClientConnected += () => Interlocked.Increment(ref connectedCount);

        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);
        using var validClient = await ConnectAsync(started.Value.Port, "/ws?token=" + expectedToken);
        await WaitForAsync(() => Volatile.Read(ref connectedCount) is 1);

        var path = token is null ? "/ws" : "/ws?token=" + token;
        await Assert.ThrowsAnyAsync<WebSocketException>(() => ConnectAsync(started.Value.Port, path));

        Assert.True(host.IsConnected);
        Assert.Equal(1, Volatile.Read(ref connectedCount));
        Assert.Equal(WebSocketState.Open, validClient.State);
    }

    [Fact]
    public async Task OversizedFragmentedTextClosesWithPolicyViolationWithoutDispatchingMessage()
    {
        const int maxMessageBytes = 32;
        await using var host = new WpsBridgeHost(maxInboundMessageBytes: maxMessageBytes);
        var received = new TaskCompletionSource<WpsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        host.OnMessageReceived += message => received.TrySetResult(message);

        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);
        using var client = await ConnectAsync(started.Value.Port, "/ws");

        using var timeout = new CancellationTokenSource(TestTimeout);
        await client.SendAsync(
            Encoding.UTF8.GetBytes(new string('x', maxMessageBytes)),
            WebSocketMessageType.Text,
            endOfMessage: false,
            timeout.Token);
        await client.SendAsync(
            Encoding.UTF8.GetBytes("overflow"),
            WebSocketMessageType.Text,
            endOfMessage: true,
            timeout.Token);

        var close = await ReceiveAsync(client);
        Assert.Equal(WebSocketMessageType.Close, close.MessageType);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, close.CloseStatus);
        Assert.False(received.Task.IsCompleted);
        Assert.False(host.IsConnected);
    }

    [Fact]
    public async Task CorrelationUsesExplicitMessageIdsAndTrackerCompletesOrTimesOut()
    {
        var protocol = new WpsProtocol();
        await using var host = new WpsBridgeHost(protocol);
        var started = await host.StartAsync(3892, 3902);
        Assert.True(started.IsSuccess, started.Error?.Message);

        using var client = await ConnectAsync(started.Value.Port, "/ws");
        var requestId = protocol.NewMessageId();
        var sent = await host.SendAsync("presentation_state_get", new { }, requestId);
        Assert.True(sent.IsSuccess, sent.Error?.Message);

        using var request = JsonDocument.Parse(await ReceiveTextAsync(client));
        Assert.Equal(requestId, request.RootElement.GetProperty("message_id").GetString());

        var tracker = new WpsRequestTracker();
        var tracked = tracker.TrackAsync(requestId, TimeSpan.FromSeconds(2));
        var response = protocol.MakeMessage(
            "command_result",
            new { command_id = requestId, ok = true, code = "OK" },
            protocol.NewMessageId());

        var completed = tracker.Complete(requestId, response);
        Assert.True(completed.IsSuccess, completed.Error?.Message);
        var trackedResult = await WithTimeout(tracked);
        Assert.True(trackedResult.IsSuccess, trackedResult.Error?.Message);
        Assert.Equal("command_result", trackedResult.Value.MessageType);
        Assert.Equal(requestId, trackedResult.Value.Payload.GetProperty("command_id").GetString());

        var timeout = await tracker.TrackAsync(protocol.NewMessageId(), TimeSpan.FromMilliseconds(20));
        Assert.False(timeout.IsSuccess);
        Assert.Contains("Timed out", timeout.Error!.Message, StringComparison.Ordinal);

        var duplicateId = protocol.NewMessageId();
        var firstTrack = tracker.TrackAsync(duplicateId, TimeSpan.FromSeconds(2));
        var duplicate = await tracker.TrackAsync(duplicateId, TimeSpan.FromMilliseconds(20));
        Assert.False(duplicate.IsSuccess);
        Assert.Contains("already tracked", duplicate.Error!.Message, StringComparison.Ordinal);
        Assert.True(tracker.Complete(duplicateId, response).IsSuccess);
        Assert.True((await WithTimeout(firstTrack)).IsSuccess);

        var unknown = tracker.Complete(protocol.NewMessageId(), response);
        Assert.False(unknown.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.NotFound, unknown.Error!.Code);
    }

    [Fact]
    public async Task MalformedMessagesReturnErrorEnvelopeAndKeepConnectionOpen()
    {
        var protocol = new WpsProtocol();
        await using var host = new WpsBridgeHost(protocol);
        var received = new TaskCompletionSource<WpsMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var protocolErrors = new List<string>();
        host.OnMessageReceived += message => received.TrySetResult(message);
        host.OnProtocolError += protocolErrors.Add;

        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);

        using var client = await ConnectAsync(started.Value.Port, "/ws");
        await SendTextAsync(client, "{not-json");
        AssertProtocolError(await ReceiveTextAsync(client), "Invalid JSON");

        await SendTextAsync(client, "{\"message_type\":\"unknown\",\"payload\":{}}");
        AssertProtocolError(await ReceiveTextAsync(client), "Unknown message_type: unknown");

        await SendTextAsync(
            client,
            """
            {"message_type":"hello","message_id":"11111111111111111111111111111111","ts":1,"payload":{"protocol_version":1,"host":{"platform":"windows","app":"WPS"},"capabilities":{"can_next":true,"can_prev":true,"can_goto":true,"can_get_state":true,"can_set_pen_color":true,"can_get_thumbnails":true}}}
            """);
        AssertProtocolError(await ReceiveTextAsync(client), "plugin_version");

        var validId = protocol.NewMessageId();
        await SendTextAsync(client, protocol.Encode("hello", HelloPayload(), validId));
        var valid = await WithTimeout(received.Task);
        Assert.Equal(validId, valid.MessageId);
        Assert.True(protocolErrors.Count >= 3);
    }

    [Fact]
    public async Task WrongPathConnectsThenClosesWithoutProtocolEnvelope()
    {
        await using var host = new WpsBridgeHost();
        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);

        using var client = await ConnectAsync(started.Value.Port, "/other");
        var close = await ReceiveAsync(client);

        Assert.Equal(WebSocketMessageType.Close, close.MessageType);
    }

    [Fact]
    public async Task SecondConnectionReplacesFirstClient()
    {
        await using var host = new WpsBridgeHost();
        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);

        using var first = await ConnectAsync(started.Value.Port, "/ws");
        using var second = await ConnectAsync(started.Value.Port, "/ws");
        var firstClose = await ReceiveAsync(first);

        Assert.Equal(WebSocketMessageType.Close, firstClose.MessageType);
        Assert.True(host.IsConnected);
        Assert.Equal(WebSocketState.Open, second.State);
    }

    [Fact]
    public async Task StopClosesClientAndReleasesPortForImmediateRebind()
    {
        await using var host = new WpsBridgeHost();
        var started = await host.StartAsync();
        Assert.True(started.IsSuccess, started.Error?.Message);
        var port = started.Value.Port;

        var alreadyStarted = await host.StartAsync();
        Assert.True(alreadyStarted.IsSuccess, alreadyStarted.Error?.Message);
        Assert.Equal(port, alreadyStarted.Value.Port);

        var notConnected = await host.SendAsync("presentation_next", new { });
        Assert.False(notConnected.IsSuccess);
        Assert.Equal(PlatformOperationErrorCode.Unavailable, notConnected.Error!.Code);
        Assert.Equal("WPS bridge is not connected", notConnected.Error.Message);

        using var client = await ConnectAsync(port, "/ws");
        var stopped = await host.StopAsync();
        Assert.True(stopped.IsSuccess, stopped.Error?.Message);
        var close = await ReceiveAsync(client);
        Assert.Equal(WebSocketMessageType.Close, close.MessageType);

        var restarted = await host.StartAsync(port, port);
        Assert.True(restarted.IsSuccess, restarted.Error?.Message);
        Assert.Equal(port, restarted.Value.Port);
        Assert.True((await host.StopAsync()).IsSuccess);

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
        listener.Start();
        Assert.True(listener.IsListening);
        listener.Stop();
    }

    [Fact]
    public void SchemaCodecDiscoversValidatesAndRoundTripsContractTypes()
    {
        var protocol = new WpsProtocol();
        var expectedTypes = new[]
        {
            "command_result",
            "error",
            "hello",
            "log",
            "presentation_goto",
            "presentation_next",
            "presentation_pen_color_set",
            "presentation_prev",
            "presentation_state",
            "presentation_state_get",
            "presentation_thumbnails",
            "presentation_thumbnails_get",
        };
        Assert.Equal(expectedTypes, protocol.MessageTypes);

        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var generated = protocol.MakeMessage("presentation_next", new { });
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.Matches("^[0-9a-f]{32}$", generated.MessageId);
        Assert.InRange(generated.Ts, before, after);

        var explicitMessage = protocol.MakeMessage("presentation_next", new { }, "0123456789abcdef0123456789abcdef", 12345);
        var decoded = protocol.Decode(protocol.Encode(explicitMessage));
        Assert.Equal(explicitMessage.MessageType, decoded.MessageType);
        Assert.Equal(explicitMessage.MessageId, decoded.MessageId);
        Assert.Equal(explicitMessage.Ts, decoded.Ts);
        Assert.Equal(JsonValueKind.Object, decoded.Payload.ValueKind);

        Assert.Throws<WpsProtocolException>(() => protocol.MakeMessage("missing_type", new { }));
        Assert.Equal(
            "Message must be a JSON object",
            Assert.Throws<WpsProtocolException>(() => protocol.Decode("[]")).Message);
        Assert.StartsWith(
            "Invalid JSON:",
            Assert.Throws<WpsProtocolException>(() => protocol.Decode("{not-json")).Message,
            StringComparison.Ordinal);
        Assert.Equal(
            "Message is missing string field 'message_type'",
            Assert.Throws<WpsProtocolException>(() => protocol.Decode("{\"payload\":{}}")).Message);

        var duplicate = Assert.Throws<WpsProtocolException>(() => new WpsProtocol(DuplicateSchemaSet()));
        Assert.Contains("Duplicate schema for message_type hello", duplicate.Message, StringComparison.Ordinal);
    }

    private static object HelloPayload() => new
    {
        plugin_version = "1.0.0",
        protocol_version = 1,
        host = new { platform = "windows", app = "WPS" },
        capabilities = CapabilitiesPayload(),
    };

    private static object CapabilitiesPayload() => new
    {
        can_next = true,
        can_prev = true,
        can_goto = true,
        can_get_state = true,
        can_set_pen_color = true,
        can_get_thumbnails = true,
    };

    private static async Task<ClientWebSocket> ConnectAsync(int port, string path)
    {
        var client = new ClientWebSocket();
        using var timeout = new CancellationTokenSource(TestTimeout);
        await client.ConnectAsync(new Uri($"ws://127.0.0.1:{port}{path}"), timeout.Token);
        return client;
    }

    private static async Task SendTextAsync(ClientWebSocket client, string text)
    {
        using var timeout = new CancellationTokenSource(TestTimeout);
        await client.SendAsync(
            Encoding.UTF8.GetBytes(text),
            WebSocketMessageType.Text,
            endOfMessage: true,
            timeout.Token);
    }

    private static async Task<string> ReceiveTextAsync(ClientWebSocket client)
    {
        var result = await ReceiveAsync(client);
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        return Encoding.UTF8.GetString(result.Bytes);
    }

    private static async Task<ReceivedWebSocketMessage> ReceiveAsync(ClientWebSocket client)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();
        using var timeout = new CancellationTokenSource(TestTimeout);

        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await client.ReceiveAsync(buffer, timeout.Token);
            }
            catch (WebSocketException)
            {
                return new ReceivedWebSocketMessage(WebSocketMessageType.Close, stream.ToArray(), client.CloseStatus);
            }

            if (result.MessageType is WebSocketMessageType.Close)
            {
                return new ReceivedWebSocketMessage(result.MessageType, stream.ToArray(), result.CloseStatus);
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                return new ReceivedWebSocketMessage(result.MessageType, stream.ToArray(), result.CloseStatus);
            }
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TestTimeout);
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task<T> WithTimeout<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
        Assert.Same(task, completed);
        return await task;
    }

    private static void AssertProtocolError(string rawText, string expectedDetail)
    {
        using var document = JsonDocument.Parse(rawText);
        var root = document.RootElement;
        Assert.Equal("error", root.GetProperty("message_type").GetString());
        var payload = root.GetProperty("payload");
        Assert.Equal("INVALID_STATE", payload.GetProperty("code").GetString());
        Assert.Equal("Invalid WPS bridge message", payload.GetProperty("message").GetString());
        var validationError = payload.GetProperty("detail").GetProperty("validation_error").GetString();
        Assert.Contains(expectedDetail, validationError, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> DuplicateSchemaSet() => new(StringComparer.Ordinal)
    {
        ["common.schema.json"] = """
            {
              "$schema": "https://json-schema.org/draft/2020-12/schema",
              "$defs": {
                "messageBase": {
                  "properties": {
                    "message_type": { "type": "string" },
                    "payload": { "type": "object" }
                  },
                  "required": ["message_type", "payload"]
                }
              }
            }
            """,
        ["hello-one.schema.json"] = DuplicateHelloSchema,
        ["hello-two.schema.json"] = DuplicateHelloSchema,
    };

    private const string DuplicateHelloSchema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "allOf": [
            { "$ref": "common.schema.json#/$defs/messageBase" },
            {
              "properties": {
                "message_type": { "const": "hello" },
                "payload": { "type": "object", "properties": {} }
              }
            }
          ],
          "unevaluatedProperties": false
        }
        """;

    private sealed record ReceivedWebSocketMessage(
        WebSocketMessageType MessageType,
        byte[] Bytes,
        WebSocketCloseStatus? CloseStatus);
}
