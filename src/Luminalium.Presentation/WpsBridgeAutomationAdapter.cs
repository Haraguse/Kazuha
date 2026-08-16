using System.Text.Json;
using Luminalium.Wps;

namespace Luminalium.Presentation;

public sealed class WpsBridgeAutomationAdapter : IWpsAutomationAdapter, IDisposable
{
    public const string AuthenticationTokenEnvironmentVariable = "LUMINALIUM_WPS_BRIDGE_TOKEN";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private readonly WpsBridgeHost _host;
    private readonly WpsRequestTracker _requestTracker;
    private readonly WpsProtocol _protocol;
    private readonly object _stateGate = new();
    private WpsBridgePresentationState _lastState = new(0, 0, false, PresentationPointerType.Unknown, "#000000");
    private TaskCompletionSource<WpsBridgePresentationState>? _stateCompletion;
    private bool _disposed;

    public WpsBridgeAutomationAdapter(
        WpsBridgeHost? host = null,
        WpsRequestTracker? requestTracker = null,
        WpsProtocol? protocol = null,
        Func<string?>? authenticationTokenProvider = null)
    {
        _protocol = protocol ?? new WpsProtocol();
        _host = host ?? new WpsBridgeHost(_protocol, RequireAuthenticationToken(authenticationTokenProvider ?? ReadAuthenticationToken));
        _requestTracker = requestTracker ?? new WpsRequestTracker();
        _host.OnMessageReceived += OnMessageReceived;
        _host.OnClientDisconnected += OnClientDisconnected;
    }

    private static string? ReadAuthenticationToken() =>
        Environment.GetEnvironmentVariable(AuthenticationTokenEnvironmentVariable);

    private static string RequireAuthenticationToken(Func<string?> tokenProvider) =>
        tokenProvider() switch
        {
            null => throw new InvalidOperationException($"{AuthenticationTokenEnvironmentVariable} must be configured for the production WPS bridge."),
            var token when token.Length is 0 => throw new ArgumentException($"{AuthenticationTokenEnvironmentVariable} must not be empty."),
            var token => token,
        };

    public bool IsConnected => _host.IsConnected;

    public async Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var start = await _host.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return start.IsSuccess
            ? PresentationOperationResult.Success()
            : PresentationOperationResult.Failure(PresentationErrors.FromPlatform(start.Error!));
    }

    public async Task<PresentationOperationResult<WpsBridgePresentationState>> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (!_host.IsConnected)
        {
            var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected.IsSuccess)
            {
                return PresentationOperation.Failure<WpsBridgePresentationState>(connected.Error!, _lastState);
            }
        }

        if (!_host.IsConnected)
        {
            return PresentationOperation.Failure<WpsBridgePresentationState>(PresentationErrors.HostUnavailable("WPS bridge is not connected."), _lastState);
        }

        var stateCompletion = new TaskCompletionSource<WpsBridgePresentationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_stateGate)
        {
            _stateCompletion = stateCompletion;
        }

        var sent = await _host.SendAsync("presentation_state_get", new { }, _protocol.NewMessageId(), cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess)
        {
            return PresentationOperation.Failure<WpsBridgePresentationState>(PresentationErrors.FromPlatform(sent.Error!), _lastState);
        }

        var completed = await Task.WhenAny(stateCompletion.Task, Task.Delay(DefaultTimeout, cancellationToken)).ConfigureAwait(false);
        if (completed == stateCompletion.Task)
        {
            return PresentationOperation.Success(await stateCompletion.Task.ConfigureAwait(false));
        }

        return PresentationOperation.Failure<WpsBridgePresentationState>(PresentationErrors.CommandRejected("Timed out waiting for WPS presentation_state."), _lastState);
    }

    public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default) =>
        SendAndTrackAsync("presentation_next", new { }, cancellationToken);

    public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
        SendAndTrackAsync("presentation_prev", new { }, cancellationToken);

    public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default) =>
        SendAndTrackAsync("presentation_goto", new { slide_index = slideIndex }, cancellationToken);

    public Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default) =>
        SendAndTrackAsync("presentation_pen_color_set", new { color = PresentationMonitor.NormalizeColorHex(colorHex) }, cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _host.OnMessageReceived -= OnMessageReceived;
        _host.OnClientDisconnected -= OnClientDisconnected;
    }

    private async Task<PresentationOperationResult> SendAndTrackAsync(
        string messageType,
        object payload,
        CancellationToken cancellationToken)
    {
        if (!_host.IsConnected)
        {
            var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected.IsSuccess)
            {
                return connected;
            }
        }

        if (!_host.IsConnected)
        {
            return PresentationOperationResult.Failure(PresentationErrors.HostUnavailable("WPS bridge is not connected."));
        }

        var messageId = _protocol.NewMessageId();
        var tracked = _requestTracker.TrackAsync(messageId, DefaultTimeout, cancellationToken);
        var sent = await _host.SendAsync(messageType, payload, messageId, cancellationToken).ConfigureAwait(false);
        if (!sent.IsSuccess)
        {
            return PresentationOperationResult.Failure(PresentationErrors.FromPlatform(sent.Error!));
        }

        var response = await tracked.ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            return PresentationOperationResult.Failure(PresentationErrors.FromPlatform(response.Error!));
        }

        if (response.Value.MessageType is "presentation_state")
        {
            return PresentationOperationResult.Success();
        }

        if (response.Value.MessageType is not "command_result")
        {
            return PresentationOperationResult.Failure(PresentationErrors.CommandRejected(
                "WPS bridge returned an unexpected response.",
                response.Value.MessageType));
        }

        return ParseCommandResult(response.Value.Payload);
    }

    private void OnMessageReceived(WpsMessage message)
    {
        if (message.MessageType is "presentation_state")
        {
            WpsBridgePresentationState state;
            TaskCompletionSource<WpsBridgePresentationState>? completion;
            lock (_stateGate)
            {
                state = ParsePresentationState(message.Payload);
                _lastState = state;
                completion = _stateCompletion;
                _stateCompletion = null;
            }

            completion?.TrySetResult(state);

            if (!string.IsNullOrWhiteSpace(message.MessageId))
            {
                _ = _requestTracker.Complete(message.MessageId, message);
            }

            return;
        }

        if (message.MessageType is "command_result")
        {
            var commandId = message.Payload.TryGetProperty("command_id", out var commandIdElement)
                ? commandIdElement.GetString() ?? string.Empty
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(commandId))
            {
                _ = _requestTracker.Complete(commandId, message);
            }
        }
    }

    private void OnClientDisconnected()
    {
        lock (_stateGate)
        {
            _lastState = _lastState with { IsSlideShow = false };
        }
    }

    private static PresentationOperationResult ParseCommandResult(JsonElement payload)
    {
        var ok = payload.TryGetProperty("ok", out var okElement) && okElement.ValueKind is JsonValueKind.True;
        if (ok)
        {
            return PresentationOperationResult.Success();
        }

        var code = payload.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? string.Empty : string.Empty;
        var message = payload.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? string.Empty : string.Empty;
        var error = code switch
        {
            "UNSUPPORTED_COMMAND" => PresentationErrors.Unsupported("WPS bridge command is unsupported.", message),
            "SLIDE_OUT_OF_RANGE" => PresentationErrors.CommandRejected("WPS bridge rejected the slide index.", message),
            "NO_ACTIVE_PRESENTATION" or "INVALID_STATE" => PresentationErrors.SlideshowClosed("WPS has no active slideshow.", message),
            "HOST_API_UNAVAILABLE" => PresentationErrors.HostUnavailable("WPS host API is unavailable.", message),
            _ => PresentationErrors.CommandRejected("WPS bridge command failed.", string.IsNullOrWhiteSpace(message) ? code : message),
        };
        return PresentationOperationResult.Failure(error);
    }

    private static WpsBridgePresentationState ParsePresentationState(JsonElement payload)
    {
        var document = payload.TryGetProperty("document", out var documentElement) && documentElement.ValueKind is JsonValueKind.Object
            ? documentElement
            : default;
        var presentation = payload.TryGetProperty("presentation", out var presentationElement) && presentationElement.ValueKind is JsonValueKind.Object
            ? presentationElement
            : default;

        var slideCount = ReadInt(document, "slide_count");
        var currentSlide = ReadInt(presentation, "current_slide");
        var isSlideShow = ReadBool(presentation, "is_slide_show");
        var pointerType = ReadPointerType(ReadString(presentation, "pointer_type"));
        var penColor = PresentationMonitor.NormalizeColorHex(ReadString(presentation, "pen_color"));

        return new WpsBridgePresentationState(currentSlide, slideCount, isSlideShow, pointerType, penColor);
    }

    private static int ReadInt(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind is JsonValueKind.Number
            && property.TryGetInt32(out var value)
                ? value
                : 0;

    private static bool ReadBool(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object
        && element.TryGetProperty(name, out var property)
        && property.ValueKind is JsonValueKind.True;

    private static string ReadString(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind is JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : string.Empty;

    private static PresentationPointerType ReadPointerType(string pointerType) =>
        pointerType.Trim().ToLowerInvariant() switch
        {
            "arrow" => PresentationPointerType.Arrow,
            "pen" => PresentationPointerType.Pen,
            "highlighter" => PresentationPointerType.Highlighter,
            "eraser" => PresentationPointerType.Eraser,
            _ => PresentationPointerType.Unknown,
        };
}
