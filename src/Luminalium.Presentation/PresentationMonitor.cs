namespace Luminalium.Presentation;

public sealed class PresentationMonitor
{
    private readonly IReadOnlyList<IPresentationHost> _hosts;
    private readonly ICommandTimer _commandTimer;
    private IPresentationHost? _activeHost;
    private PresentationState _state = PresentationState.Empty;
    private PresentationPointerType _activePointerType = PresentationPointerType.Arrow;
    private PresentationPointerType _lastNonEraserPointerType = PresentationPointerType.Arrow;

    public PresentationMonitor(
        IEnumerable<IPresentationHost> hosts,
        ICommandTimer? commandTimer = null,
        PresentationHostKind preferredKind = PresentationHostKind.PowerPoint)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        _hosts = OrderHosts(hosts, preferredKind).ToArray();
        _commandTimer = commandTimer ?? new SlidingWindowCommandTimer();
    }

    public PresentationState CurrentState => _state;

    public PresentationHostKind ActiveHostKind => _activeHost?.HostKind ?? PresentationHostKind.None;

    public async Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        PresentationError? lastError = null;
        foreach (var host in _hosts)
        {
            var connect = await GuardAsync(
                () => host.ConnectAsync(cancellationToken),
                $"connecting to {host.HostKind}").ConfigureAwait(false);
            if (!connect.IsSuccess)
            {
                lastError = connect.Error;
                continue;
            }

            var state = await GuardAsync(
                () => host.GetStateAsync(cancellationToken),
                $"reading state from {host.HostKind}",
                PresentationState.Empty with { HostKind = host.HostKind }).ConfigureAwait(false);
            if (!state.IsSuccess)
            {
                lastError = state.Error;
                continue;
            }

            _activeHost = host;
            CacheState(state.Value);
            return PresentationOperationResult.Success();
        }

        return PresentationOperationResult.Failure(lastError ?? PresentationErrors.HostUnavailable("No presentation host is available."));
    }

    public async Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (_activeHost is not null)
        {
            var activeState = await GuardAsync(
                () => _activeHost.GetStateAsync(cancellationToken),
                $"reading state from {_activeHost.HostKind}",
                _state).ConfigureAwait(false);
            if (activeState.IsSuccess)
            {
                CacheState(activeState.Value);
                return activeState;
            }

            if (activeState.Error?.Code is PresentationErrorCode.SlideshowClosed or PresentationErrorCode.DisconnectedHost or PresentationErrorCode.HostUnavailable)
            {
                _activeHost = null;
            }
            else
            {
                return PresentationOperation.Failure<PresentationState>(activeState.Error!, _state);
            }
        }

        var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        return connected.IsSuccess
            ? PresentationOperation.Success(_state)
            : PresentationOperation.Failure<PresentationState>(connected.Error!, _state);
    }

    public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default) =>
        NavigateAsync(PresentationNavigationCommand.Next, cancellationToken);

    public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
        NavigateAsync(PresentationNavigationCommand.Previous, cancellationToken);

    public async Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default)
    {
        if (slideIndex < 1)
        {
            return PresentationOperationResult.Failure(PresentationErrors.CommandRejected("Slide indexes are one-based."));
        }

        var host = await GetActiveHostAsync(cancellationToken).ConfigureAwait(false);
        if (!host.IsSuccess)
        {
            return PresentationOperationResult.Failure(host.Error!);
        }

        var restricted = GetRestrictionFailure();
        if (restricted is not null)
        {
            return PresentationOperationResult.Failure(restricted);
        }

        var result = await GuardAsync(
            () => host.Value.GotoSlideAsync(slideIndex, cancellationToken),
            $"going to slide {slideIndex} on {host.Value.HostKind}").ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result;
        }

        await RestorePointerAfterNavigationAsync(host.Value, cancellationToken).ConfigureAwait(false);
        await RefreshStateBestEffortAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default)
    {
        var host = await GetActiveHostAsync(cancellationToken).ConfigureAwait(false);
        if (!host.IsSuccess)
        {
            return PresentationOperationResult.Failure(host.Error!);
        }

        var normalized = NormalizeColorHex(colorHex);
        var result = await GuardAsync(
            () => host.Value.ApplyPenColorAsync(normalized, cancellationToken),
            $"applying pen color on {host.Value.HostKind}").ConfigureAwait(false);
        if (result.IsSuccess)
        {
            _state = _state with { PenColor = normalized };
        }

        return result;
    }

    public async Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default)
    {
        var host = await GetActiveHostAsync(cancellationToken).ConfigureAwait(false);
        if (!host.IsSuccess)
        {
            return PresentationOperationResult.Failure(host.Error!);
        }

        var result = await GuardAsync(
            () => host.Value.SetPointerTypeAsync(pointerType, cancellationToken),
            $"setting pointer type on {host.Value.HostKind}").ConfigureAwait(false);
        if (result.IsSuccess)
        {
            RememberPointerType(pointerType);
            _state = _state with { PointerType = pointerType };
        }
        else if (pointerType is PresentationPointerType.Eraser)
        {
            _state = _state with { PointerType = _lastNonEraserPointerType };
        }

        return result;
    }

    public async Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default)
    {
        var host = await GetActiveHostAsync(cancellationToken).ConfigureAwait(false);
        return host.IsSuccess
            ? await GuardAsync(
                () => host.Value.ZoomAsync(zoomFactor, cancellationToken),
                $"using zoom seam on {host.Value.HostKind}").ConfigureAwait(false)
            : PresentationOperationResult.Failure(host.Error!);
    }

    public static string NormalizeColorHex(string colorHex)
    {
        var value = (colorHex ?? string.Empty).Trim();
        if (value.StartsWith('#'))
        {
            value = value[1..];
        }

        if (value.Length != 6 || value.Any(character => !Uri.IsHexDigit(character)))
        {
            return "#000000";
        }

        return "#" + value.ToUpperInvariant();
    }

    private async Task<PresentationOperationResult> NavigateAsync(
        PresentationNavigationCommand command,
        CancellationToken cancellationToken)
    {
        var token = _commandTimer.TryConsumePageTurn();
        if (!token.IsSuccess)
        {
            return token;
        }

        var host = await GetActiveHostAsync(cancellationToken).ConfigureAwait(false);
        if (!host.IsSuccess)
        {
            return PresentationOperationResult.Failure(host.Error!);
        }

        var restricted = GetRestrictionFailure();
        if (restricted is not null)
        {
            return PresentationOperationResult.Failure(restricted);
        }

        var result = command switch
        {
            PresentationNavigationCommand.Next => await GuardAsync(
                () => host.Value.NavigateNextAsync(cancellationToken),
                $"navigating next on {host.Value.HostKind}").ConfigureAwait(false),
            PresentationNavigationCommand.Previous => await GuardAsync(
                () => host.Value.NavigatePreviousAsync(cancellationToken),
                $"navigating previous on {host.Value.HostKind}").ConfigureAwait(false),
            _ => PresentationOperationResult.Failure(PresentationErrors.Unsupported("Unknown navigation command.")),
        };

        if (!result.IsSuccess)
        {
            return result;
        }

        await RestorePointerAfterNavigationAsync(host.Value, cancellationToken).ConfigureAwait(false);
        await RefreshStateBestEffortAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task<PresentationOperationResult<IPresentationHost>> GetActiveHostAsync(CancellationToken cancellationToken)
    {
        if (_activeHost is not null)
        {
            return PresentationOperation.Success(_activeHost);
        }

        var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        return connected.IsSuccess
            ? PresentationOperation.Success(_activeHost!)
            : PresentationOperation.Failure<IPresentationHost>(connected.Error!);
    }

    private async Task RestorePointerAfterNavigationAsync(IPresentationHost host, CancellationToken cancellationToken)
    {
        if (_activePointerType is not (PresentationPointerType.Pen or PresentationPointerType.Highlighter or PresentationPointerType.Eraser))
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken).ConfigureAwait(false);
        var result = await GuardAsync(
            () => host.SetPointerTypeAsync(_activePointerType, cancellationToken),
            $"restoring pointer type on {host.HostKind}").ConfigureAwait(false);
        if (result.IsSuccess)
        {
            _state = _state with { PointerType = _activePointerType };
        }
    }

    private async Task RefreshStateBestEffortAsync(CancellationToken cancellationToken)
    {
        if (_activeHost is null)
        {
            return;
        }

        var state = await GuardAsync(
            () => _activeHost.GetStateAsync(cancellationToken),
            $"refreshing state from {_activeHost.HostKind}",
            _state).ConfigureAwait(false);
        if (state.IsSuccess)
        {
            CacheState(state.Value);
        }
    }

    private PresentationError? GetRestrictionFailure() =>
        _state.IsProtectedView
            ? PresentationErrors.ProtectedViewRestricted(
                "The presentation cannot be controlled while Protected View restrictions are active.",
                $"ProtectedView={_state.IsProtectedView}; ReadOnly={_state.IsReadOnly}")
            : null;

    private void CacheState(PresentationState state)
    {
        _state = state with { PenColor = NormalizeColorHex(state.PenColor) };
        if (state.PointerType is not PresentationPointerType.Unknown)
        {
            RememberPointerType(state.PointerType);
        }
    }

    private void RememberPointerType(PresentationPointerType pointerType)
    {
        if (pointerType is PresentationPointerType.Arrow or PresentationPointerType.Pen or PresentationPointerType.Highlighter or PresentationPointerType.Eraser)
        {
            _activePointerType = pointerType;
        }

        if (pointerType is PresentationPointerType.Arrow or PresentationPointerType.Pen or PresentationPointerType.Highlighter)
        {
            _lastNonEraserPointerType = pointerType;
        }
    }

    private static IEnumerable<IPresentationHost> OrderHosts(IEnumerable<IPresentationHost> hosts, PresentationHostKind preferredKind)
    {
        var hostList = hosts.ToList();
        return preferredKind is PresentationHostKind.None
            ? hostList
            : hostList.OrderBy(host => host.HostKind == preferredKind ? 0 : 1).ThenBy(host => host.HostKind);
    }

    private static async Task<PresentationOperationResult> GuardAsync(
        Func<Task<PresentationOperationResult>> operation,
        string context)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PresentationOperationResult.Failure(PresentationErrors.FromException(exception, context));
        }
    }

    private static async Task<PresentationOperationResult<T>> GuardAsync<T>(
        Func<Task<PresentationOperationResult<T>>> operation,
        string context,
        T fallback)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PresentationOperation.Failure(PresentationErrors.FromException(exception, context), fallback);
        }
    }
}
