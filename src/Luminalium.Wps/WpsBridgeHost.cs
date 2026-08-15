using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Luminalium.Core.Platform;

namespace Luminalium.Wps;

public sealed class WpsBridgeHost : IAsyncDisposable
{
    private const string BridgePath = "/ws";
    private const int ReceiveBufferSize = 16 * 1024;
    private const int DefaultMaxInboundMessageBytes = 1 * 1024 * 1024;

    private readonly WpsProtocol _protocol;
    private readonly string? _authenticationToken;
    private readonly int _maxInboundMessageBytes;
    private readonly object _stateGate = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _listenerCancellation;
    private Task? _acceptLoop;
    private ClientConnection? _client;
    private int _port;

    /// <summary>
    /// Creates a WPS bridge host. A null authentication token explicitly enables
    /// the legacy unauthenticated compatibility mode for existing callers.
    /// </summary>
    public WpsBridgeHost(
        WpsProtocol? protocol = null,
        string? authenticationToken = null,
        int maxInboundMessageBytes = DefaultMaxInboundMessageBytes)
    {
        if (authenticationToken is not null && authenticationToken.Length is 0)
        {
            throw new ArgumentException("Authentication token must not be empty.", nameof(authenticationToken));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxInboundMessageBytes);

        _protocol = protocol ?? new WpsProtocol();
        _authenticationToken = authenticationToken;
        _maxInboundMessageBytes = maxInboundMessageBytes;
    }

    public event Action? OnClientConnected;

    public event Action? OnClientDisconnected;

    public event Action<WpsMessage>? OnMessageReceived;

    public event Action<string>? OnProtocolError;

    public bool IsRunning
    {
        get
        {
            lock (_stateGate)
            {
                return _listener is not null && _listener.IsListening;
            }
        }
    }

    public bool IsConnected
    {
        get
        {
            lock (_stateGate)
            {
                return _client?.Socket.State is WebSocketState.Open;
            }
        }
    }

    public int Port
    {
        get
        {
            lock (_stateGate)
            {
                return _port;
            }
        }
    }

    public Task<PlatformOperationResult<WpsBridgeStartInfo>> StartAsync(
        int portStart = 3892,
        int portEnd = 3902,
        CancellationToken cancellationToken = default)
    {
        if (!HttpListener.IsSupported)
        {
            return Task.FromResult(Failure<WpsBridgeStartInfo>(
                PlatformOperationErrorCode.Unavailable,
                "HttpListener is not supported in this environment"));
        }

        lock (_stateGate)
        {
            if (_listener is not null && _listener.IsListening)
            {
                return Task.FromResult(PlatformOperation.Success(new WpsBridgeStartInfo(_port)));
            }
        }

        if (portStart > portEnd)
        {
            return Task.FromResult(Failure<WpsBridgeStartInfo>(
                PlatformOperationErrorCode.Failed,
                $"Invalid WPS bridge port range: {portStart}-{portEnd}"));
        }

        for (var port = portStart; port <= portEnd; port++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var listener = CreateListener(port);
            try
            {
                listener.Start();
            }
            catch (Exception exc) when (exc is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                listener.Close();
                continue;
            }

            var listenerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (_stateGate)
            {
                if (_listener is not null && _listener.IsListening)
                {
                    listener.Close();
                    listenerCancellation.Dispose();
                    return Task.FromResult(PlatformOperation.Success(new WpsBridgeStartInfo(_port)));
                }

                _listener = listener;
                _listenerCancellation = listenerCancellation;
                _port = port;
                _acceptLoop = Task.Run(() => AcceptLoopAsync(listener, listenerCancellation.Token), CancellationToken.None);
            }

            return Task.FromResult(PlatformOperation.Success(new WpsBridgeStartInfo(port)));
        }

        return Task.FromResult(Failure<WpsBridgeStartInfo>(
            PlatformOperationErrorCode.Unavailable,
            $"No available WPS bridge port in range {portStart}-{portEnd}"));
    }

    public async Task<PlatformOperationResult> StopAsync()
    {
        HttpListener? listener;
        CancellationTokenSource? listenerCancellation;
        Task? acceptLoop;
        ClientConnection? client;

        lock (_stateGate)
        {
            listener = _listener;
            listenerCancellation = _listenerCancellation;
            acceptLoop = _acceptLoop;
            client = _client;
            _listener = null;
            _listenerCancellation = null;
            _acceptLoop = null;
            _client = null;
            _port = 0;
        }

        await CloseConnectionAsync(client, WebSocketCloseStatus.NormalClosure, "WPS bridge host stopped").ConfigureAwait(false);

        try
        {
            listenerCancellation?.Cancel();
            listener?.Stop();
            listener?.Close();
            if (acceptLoop is not null)
            {
                await acceptLoop.ConfigureAwait(false);
            }
        }
        catch (Exception exc) when (exc is HttpListenerException or ObjectDisposedException)
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "WPS bridge host failed while stopping",
                exc.Message));
        }
        finally
        {
            listenerCancellation?.Dispose();
        }

        return PlatformOperationResult.Success();
    }

    public async Task<PlatformOperationResult<string>> SendAsync(
        string messageType,
        object? payload = null,
        string? messageId = null,
        CancellationToken cancellationToken = default)
    {
        ClientConnection? client;
        lock (_stateGate)
        {
            client = _client;
        }

        if (client?.Socket.State is not WebSocketState.Open)
        {
            return Failure<string>(
                PlatformOperationErrorCode.Unavailable,
                "WPS bridge is not connected");
        }

        WpsMessage message;
        try
        {
            message = _protocol.MakeMessage(messageType, payload, messageId);
        }
        catch (WpsProtocolException exc)
        {
            return Failure<string>(PlatformOperationErrorCode.Failed, exc.Message);
        }

        var sendResult = await SendMessageAsync(client, message, cancellationToken).ConfigureAwait(false);
        return sendResult.IsSuccess
            ? PlatformOperation.Success(message.MessageId)
            : PlatformOperation.Failure<string>(sendResult.Error!);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private static HttpListener CreateListener(int port)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
        listener.Prefixes.Add($"http://localhost:{port}/ws/");
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Prefixes.Add($"http://localhost:{port}/");
        return listener;
    }

    private async Task AcceptLoopAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception exc) when (exc is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                break;
            }

            _ = Task.Run(() => HandleContextAsync(context, cancellationToken), CancellationToken.None);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Close();
            return;
        }

        var requestPath = context.Request.Url?.AbsolutePath ?? string.Empty;
        if (!string.Equals(requestPath, BridgePath, StringComparison.Ordinal))
        {
            var rejected = await AcceptWebSocketSafelyAsync(context).ConfigureAwait(false);
            if (rejected is not null)
            {
                await CloseSocketAsync(rejected.WebSocket, WebSocketCloseStatus.NormalClosure, "Invalid WPS bridge path", CancellationToken.None).ConfigureAwait(false);
                rejected.WebSocket.Dispose();
            }

            return;
        }

        if (!IsAuthenticationValid(context.Request.QueryString["token"]))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Close();
            return;
        }

        var webSocketContext = await AcceptWebSocketSafelyAsync(context).ConfigureAwait(false);
        if (webSocketContext is null)
        {
            return;
        }

        var connection = new ClientConnection(webSocketContext.WebSocket);
        ClientConnection? replaced;
        lock (_stateGate)
        {
            replaced = _client;
            _client = connection;
        }

        await CloseConnectionAsync(replaced, WebSocketCloseStatus.NormalClosure, "WPS bridge client replaced").ConfigureAwait(false);
        OnClientConnected?.Invoke();
        await ReceiveLoopAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HttpListenerWebSocketContext?> AcceptWebSocketSafelyAsync(HttpListenerContext context)
    {
        try
        {
            return await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Close();
            return null;
        }
    }

    private async Task ReceiveLoopAsync(ClientConnection connection, CancellationToken hostCancellationToken)
    {
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            hostCancellationToken,
            connection.Cancellation.Token);
        var cancellationToken = linkedCancellation.Token;

        try
        {
            while (!cancellationToken.IsCancellationRequested && connection.Socket.State is WebSocketState.Open)
            {
                var rawText = await ReceiveTextAsync(connection.Socket, _maxInboundMessageBytes, cancellationToken).ConfigureAwait(false);
                if (rawText is null)
                {
                    break;
                }

                try
                {
                    var message = _protocol.Decode(rawText);
                    OnMessageReceived?.Invoke(message);
                }
                catch (WpsProtocolException exc)
                {
                    OnProtocolError?.Invoke(exc.Message);
                    await SendProtocolErrorAsync(connection, exc.Message, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            var disconnect = false;
            lock (_stateGate)
            {
                if (ReferenceEquals(_client, connection))
                {
                    _client = null;
                    disconnect = true;
                }
            }

            connection.Dispose();
            if (disconnect)
            {
                OnClientDisconnected?.Invoke();
            }
        }
    }

    private bool IsAuthenticationValid(string? suppliedToken)
    {
        if (_authenticationToken is null)
        {
            return true;
        }

        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(_authenticationToken));
        var suppliedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }

    private static async Task<string?> ReceiveTextAsync(
        WebSocket socket,
        int maxMessageBytes,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType is WebSocketMessageType.Close)
            {
                await CloseSocketAsync(socket, WebSocketCloseStatus.NormalClosure, "WPS bridge client closed", CancellationToken.None).ConfigureAwait(false);
                return null;
            }

            if (result.MessageType is not WebSocketMessageType.Text)
            {
                await CloseSocketAsync(
                    socket,
                    WebSocketCloseStatus.PolicyViolation,
                    "WPS bridge accepts text messages only",
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            }

            if (result.Count > maxMessageBytes - stream.Length)
            {
                await CloseSocketAsync(
                    socket,
                    WebSocketCloseStatus.PolicyViolation,
                    "WPS bridge message exceeds the maximum size",
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            }

            if (result.Count > 0)
            {
                stream.Write(buffer, 0, result.Count);
            }

            if (result.EndOfMessage)
            {
                return Encoding.UTF8.GetString(stream.GetBuffer(), 0, checked((int)stream.Length));
            }

            if (stream.Length == maxMessageBytes)
            {
                await CloseSocketAsync(
                    socket,
                    WebSocketCloseStatus.PolicyViolation,
                    "WPS bridge message exceeds the maximum size",
                    CancellationToken.None).ConfigureAwait(false);
                return null;
            }
        }
    }

    private Task<PlatformOperationResult> SendProtocolErrorAsync(
        ClientConnection connection,
        string validationError,
        CancellationToken cancellationToken) =>
        SendMessageAsync(
            connection,
            _protocol.MakeMessage(
                "error",
                new
                {
                    code = "INVALID_STATE",
                    message = "Invalid WPS bridge message",
                    detail = new Dictionary<string, object?>
                    {
                        ["validation_error"] = validationError,
                    },
                }),
            cancellationToken);

    private async Task<PlatformOperationResult> SendMessageAsync(
        ClientConnection connection,
        WpsMessage message,
        CancellationToken cancellationToken)
    {
        var raw = _protocol.Encode(message);
        var bytes = Encoding.UTF8.GetBytes(raw);

        var enteredSendGate = false;
        try
        {
            await connection.SendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredSendGate = true;
            if (connection.Socket.State is not WebSocketState.Open)
            {
                return PlatformOperationResult.Failure(new PlatformOperationError(
                    PlatformOperationErrorCode.Unavailable,
                    "WPS bridge is not connected"));
            }

            await connection.Socket.SendAsync(
                bytes,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);
            return PlatformOperationResult.Success();
        }
        catch (Exception exc) when (exc is WebSocketException or OperationCanceledException)
        {
            return PlatformOperationResult.Failure(new PlatformOperationError(
                PlatformOperationErrorCode.Failed,
                "Failed to send WPS bridge message",
                exc.Message));
        }
        finally
        {
            if (enteredSendGate)
            {
                connection.SendGate.Release();
            }
        }
    }

    private async Task CloseConnectionAsync(
        ClientConnection? connection,
        WebSocketCloseStatus closeStatus,
        string statusDescription)
    {
        if (connection is null)
        {
            return;
        }

        await CloseSocketAsync(connection.Socket, closeStatus, statusDescription, CancellationToken.None).ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);
        connection.Cancel();
        OnClientDisconnected?.Invoke();
    }

    private static async Task CloseSocketAsync(
        WebSocket socket,
        WebSocketCloseStatus closeStatus,
        string statusDescription,
        CancellationToken cancellationToken)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(250));
            try
            {
                await socket.CloseOutputAsync(closeStatus, statusDescription, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (WebSocketException)
            {
            }
        }
    }

    private static PlatformOperationResult<T> Failure<T>(
        PlatformOperationErrorCode code,
        string message,
        string? detail = null) =>
        PlatformOperation.Failure<T>(new PlatformOperationError(code, message, detail));

    private sealed class ClientConnection : IDisposable
    {
        private int _disposed;

        public ClientConnection(WebSocket socket)
        {
            Socket = socket;
        }

        public WebSocket Socket { get; }

        public SemaphoreSlim SendGate { get; } = new(1, 1);

        public CancellationTokenSource Cancellation { get; } = new();

        public void Cancel()
        {
            try
            {
                Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) is 1)
            {
                return;
            }

            Cancellation.Dispose();
            SendGate.Dispose();
            Socket.Dispose();
        }
    }
}

public sealed record WpsBridgeStartInfo(int Port);
