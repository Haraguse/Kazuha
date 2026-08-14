namespace Luminalium.Presentation;

public sealed class PowerPointPresentationHost : IPresentationHost
{
    private readonly IOfficeComAdapter _comAdapter;
    private readonly ISlideshowWindowAdapter _windowAdapter;
    private SlideshowConnection? _connection;

    public PowerPointPresentationHost(IOfficeComAdapter comAdapter, ISlideshowWindowAdapter windowAdapter)
    {
        ArgumentNullException.ThrowIfNull(comAdapter);
        ArgumentNullException.ThrowIfNull(windowAdapter);
        _comAdapter = comAdapter;
        _windowAdapter = windowAdapter;
    }

    public PresentationHostKind HostKind => PresentationHostKind.PowerPoint;

    public async Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _comAdapter.ConnectPowerPointAsync(PresentationHostKind.PowerPoint, cancellationToken).ConfigureAwait(false);
        if (connection.IsSuccess)
        {
            _connection = connection.Value;
            return PresentationOperationResult.Success();
        }

        var window = await _windowAdapter.FindSlideshowWindowAsync(PresentationHostKind.PowerPoint, cancellationToken).ConfigureAwait(false);
        if (window.IsSuccess && window.Value is not null)
        {
            _connection = new SlideshowConnection(new object(), window.Value.Hwnd, PresentationHostKind.PowerPoint);
            return PresentationOperationResult.Success();
        }

        return PresentationOperationResult.Failure(window.Error ?? connection.Error!);
    }

    public async Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected.IsSuccess)
            {
                return PresentationOperation.Failure<PresentationState>(connected.Error!, PresentationState.Empty);
            }
        }

        if (_connection is { SlideshowWindow: not null } && _connection.SlideshowWindow.GetType() != typeof(object))
        {
            var state = await _comAdapter.GetStateAsync(_connection, cancellationToken).ConfigureAwait(false);
            if (state.IsSuccess)
            {
                return state;
            }
        }

        var window = await _windowAdapter.FindSlideshowWindowAsync(PresentationHostKind.PowerPoint, cancellationToken).ConfigureAwait(false);
        if (!window.IsSuccess)
        {
            return PresentationOperation.Failure<PresentationState>(window.Error!, PresentationState.Empty with { HostKind = PresentationHostKind.PowerPoint });
        }

        if (window.Value is null)
        {
            return PresentationOperation.Failure<PresentationState>(PresentationErrors.SlideshowClosed(), PresentationState.Empty with { HostKind = PresentationHostKind.PowerPoint });
        }

        _connection = new SlideshowConnection(new object(), window.Value.Hwnd, PresentationHostKind.PowerPoint);
        return PresentationOperation.Success(PresentationState.Empty with
        {
            IsSlideShow = true,
            HostKind = PresentationHostKind.PowerPoint,
        });
    }

    public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default) =>
        NavigateWithFallbackAsync(
            connection => _comAdapter.NavigateNextAsync(connection, cancellationToken),
            [PresentationVirtualKey.Down, PresentationVirtualKey.Next],
            cancellationToken);

    public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
        NavigateWithFallbackAsync(
            connection => _comAdapter.NavigatePreviousAsync(connection, cancellationToken),
            [PresentationVirtualKey.Prior, PresentationVirtualKey.Up],
            cancellationToken);

    public async Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default)
    {
        if (slideIndex < 1)
        {
            return PresentationOperationResult.Failure(PresentationErrors.CommandRejected("Slide indexes are one-based."));
        }

        var connection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (!connection.IsSuccess)
        {
            return PresentationOperationResult.Failure(connection.Error!);
        }

        if (HasComWindow(connection.Value))
        {
            var comResult = await _comAdapter.GotoSlideAsync(connection.Value, slideIndex, cancellationToken).ConfigureAwait(false);
            if (comResult.IsSuccess)
            {
                return comResult;
            }
        }

        var hwnd = await ResolveHwndAsync(cancellationToken).ConfigureAwait(false);
        if (!hwnd.IsSuccess)
        {
            return PresentationOperationResult.Failure(hwnd.Error!);
        }

        foreach (var digit in slideIndex.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            var key = (PresentationVirtualKey)(digit - '0' + 0x30);
            var postedDigit = await _windowAdapter.PostKeyAsync(hwnd.Value, key, cancellationToken).ConfigureAwait(false);
            if (!postedDigit.IsSuccess)
            {
                return postedDigit;
            }
        }

        return await _windowAdapter.PostKeyAsync(hwnd.Value, PresentationVirtualKey.Return, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (!connection.IsSuccess)
        {
            return PresentationOperationResult.Failure(connection.Error!);
        }

        if (HasComWindow(connection.Value))
        {
            var comResult = await _comAdapter.ApplyPenColorAsync(connection.Value, colorHex, cancellationToken).ConfigureAwait(false);
            if (comResult.IsSuccess)
            {
                return comResult;
            }

            var paletteResult = await _comAdapter.ApplyPenColorViaPaletteAsync(connection.Value, colorHex, _windowAdapter, cancellationToken).ConfigureAwait(false);
            if (paletteResult.IsSuccess)
            {
                return paletteResult;
            }

            return paletteResult.Error is not null ? paletteResult : comResult;
        }

        return PresentationOperationResult.Failure(PresentationErrors.Unsupported("Pen color changes require COM automation."));
    }

    public async Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default)
    {
        var connection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (connection.IsSuccess && HasComWindow(connection.Value))
        {
            var comResult = await _comAdapter.SetPointerTypeAsync(connection.Value, pointerType, cancellationToken).ConfigureAwait(false);
            if (comResult.IsSuccess)
            {
                return comResult;
            }
        }

        var hwnd = await ResolveHwndAsync(cancellationToken).ConfigureAwait(false);
        if (!hwnd.IsSuccess)
        {
            return PresentationOperationResult.Failure(hwnd.Error!);
        }

        var shortcut = pointerType switch
        {
            PresentationPointerType.Arrow => PresentationVirtualKey.A,
            PresentationPointerType.Pen => PresentationVirtualKey.P,
            PresentationPointerType.Highlighter => PresentationVirtualKey.I,
            PresentationPointerType.Eraser => PresentationVirtualKey.E,
            _ => (PresentationVirtualKey)0,
        };
        if (shortcut == 0)
        {
            return PresentationOperationResult.Failure(PresentationErrors.Unsupported("Unknown pointer type cannot be applied."));
        }

        if (pointerType is PresentationPointerType.Pen or PresentationPointerType.Highlighter)
        {
            _ = await _windowAdapter.PostCtrlShortcutAsync(hwnd.Value, PresentationVirtualKey.A, cancellationToken).ConfigureAwait(false);
        }

        return await _windowAdapter.PostCtrlShortcutAsync(hwnd.Value, shortcut, cancellationToken).ConfigureAwait(false);
    }

    public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default) =>
        Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.Unsupported(
            "Presentation host zoom is an integration seam; overlay zoom is implemented outside the monitor boundary.",
            zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture))));

    private static bool HasComWindow(SlideshowConnection connection) =>
        connection.SlideshowWindow.GetType() != typeof(object);

    private async Task<PresentationOperationResult<SlideshowConnection>> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return PresentationOperation.Success(_connection);
        }

        var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        return connected.IsSuccess
            ? PresentationOperation.Success(_connection!)
            : PresentationOperation.Failure<SlideshowConnection>(connected.Error!);
    }

    private async Task<PresentationOperationResult> NavigateWithFallbackAsync(
        Func<SlideshowConnection, Task<PresentationOperationResult>> comNavigation,
        IReadOnlyList<PresentationVirtualKey> fallbackKeys,
        CancellationToken cancellationToken)
    {
        var connection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        if (!connection.IsSuccess)
        {
            return PresentationOperationResult.Failure(connection.Error!);
        }

        if (HasComWindow(connection.Value))
        {
            var comResult = await comNavigation(connection.Value).ConfigureAwait(false);
            if (comResult.IsSuccess)
            {
                return comResult;
            }
        }

        var hwnd = await ResolveHwndAsync(cancellationToken).ConfigureAwait(false);
        if (!hwnd.IsSuccess)
        {
            return PresentationOperationResult.Failure(hwnd.Error!);
        }

        PresentationOperationResult? lastFailure = null;
        foreach (var fallbackKey in fallbackKeys)
        {
            var posted = await _windowAdapter.PostKeyAsync(hwnd.Value, fallbackKey, cancellationToken).ConfigureAwait(false);
            if (posted.IsSuccess)
            {
                return posted;
            }

            lastFailure = posted;
        }

        return lastFailure ?? PresentationOperationResult.Failure(PresentationErrors.CommandRejected("No navigation fallback was available."));
    }

    private async Task<PresentationOperationResult<nint>> ResolveHwndAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _connection.Hwnd != 0)
        {
            return PresentationOperation.Success(_connection.Hwnd);
        }

        var window = await _windowAdapter.FindSlideshowWindowAsync(PresentationHostKind.PowerPoint, cancellationToken).ConfigureAwait(false);
        if (!window.IsSuccess)
        {
            return PresentationOperation.Failure<nint>(window.Error!);
        }

        if (window.Value is null || window.Value.Hwnd == 0)
        {
            return PresentationOperation.Failure<nint>(PresentationErrors.SlideshowClosed());
        }

        _connection = new SlideshowConnection(_connection?.SlideshowWindow ?? new object(), window.Value.Hwnd, PresentationHostKind.PowerPoint);
        return PresentationOperation.Success(window.Value.Hwnd);
    }
}

public sealed class WpsPresentationHost : IPresentationHost
{
    private readonly IWpsAutomationAdapter _wpsAdapter;

    public WpsPresentationHost(IWpsAutomationAdapter wpsAdapter)
    {
        ArgumentNullException.ThrowIfNull(wpsAdapter);
        _wpsAdapter = wpsAdapter;
    }

    public PresentationHostKind HostKind => PresentationHostKind.Wps;

    public Task<PresentationOperationResult> ConnectAsync(CancellationToken cancellationToken = default) =>
        _wpsAdapter.ConnectAsync(cancellationToken);

    public async Task<PresentationOperationResult<PresentationState>> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var state = await _wpsAdapter.GetStateAsync(cancellationToken).ConfigureAwait(false);
        if (!state.IsSuccess)
        {
            return PresentationOperation.Failure<PresentationState>(state.Error!, PresentationState.Empty with { HostKind = PresentationHostKind.Wps });
        }

        return PresentationOperation.Success(new PresentationState(
            state.Value.CurrentSlide,
            state.Value.SlideCount,
            state.Value.IsSlideShow,
            state.Value.PointerType,
            PresentationMonitor.NormalizeColorHex(state.Value.PenColor),
            PresentationHostKind.Wps,
            false,
            false));
    }

    public Task<PresentationOperationResult> NavigateNextAsync(CancellationToken cancellationToken = default) =>
        _wpsAdapter.NavigateNextAsync(cancellationToken);

    public Task<PresentationOperationResult> NavigatePreviousAsync(CancellationToken cancellationToken = default) =>
        _wpsAdapter.NavigatePreviousAsync(cancellationToken);

    public Task<PresentationOperationResult> GotoSlideAsync(int slideIndex, CancellationToken cancellationToken = default) =>
        _wpsAdapter.GotoSlideAsync(slideIndex, cancellationToken);

    public Task<PresentationOperationResult> ApplyPenColorAsync(string colorHex, CancellationToken cancellationToken = default) =>
        _wpsAdapter.ApplyPenColorAsync(PresentationMonitor.NormalizeColorHex(colorHex), cancellationToken);

    public Task<PresentationOperationResult> SetPointerTypeAsync(PresentationPointerType pointerType, CancellationToken cancellationToken = default) =>
        pointerType is PresentationPointerType.Unknown
            ? Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.Unsupported("Unknown pointer type cannot be applied through WPS.")))
            : Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.Unsupported("WPS bridge does not expose pointer-type commands in the Task 10 protocol.")));

    public Task<PresentationOperationResult> ZoomAsync(double zoomFactor, CancellationToken cancellationToken = default) =>
        Task.FromResult(PresentationOperationResult.Failure(PresentationErrors.Unsupported(
            "Presentation host zoom is an integration seam; overlay zoom is implemented outside the monitor boundary.",
            zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture))));
}
