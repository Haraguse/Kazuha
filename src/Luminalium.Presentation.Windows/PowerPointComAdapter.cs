using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Luminalium.Presentation;

namespace Luminalium.Presentation.Windows;

public sealed class PowerPointComAdapter : IOfficeComAdapter
{
    private const string PowerPointProgId = "PowerPoint.Application";
    private static readonly TimeSpan ComTimeout = TimeSpan.FromSeconds(2);

    private static readonly Dictionary<string, PenColorPaletteCommand> PenColorPalette = new(StringComparer.OrdinalIgnoreCase)
    {
        ["#FFFFFF"] = Palette(0, 0),
        ["#000000"] = Palette(0, 1),
        ["#E7E6E6"] = Palette(0, 2),
        ["#44546A"] = Palette(0, 3),
        ["#4472C4"] = Palette(0, 4),
        ["#ED7D31"] = Palette(0, 5),
        ["#A5A5A5"] = Palette(0, 6),
        ["#FFC000"] = Palette(0, 7),
        ["#5B9BD5"] = Palette(0, 8),
        ["#70AD47"] = Palette(0, 9),
        ["#C00000"] = Palette(1, 0),
        ["#FF0000"] = Palette(1, 1),
        ["#FFFF00"] = Palette(1, 3),
        ["#92D050"] = Palette(1, 4),
        ["#00B050"] = Palette(1, 5),
        ["#00B0F0"] = Palette(1, 6),
        ["#0070C0"] = Palette(1, 7),
        ["#002060"] = Palette(1, 8),
        ["#7030A0"] = Palette(1, 9),
    };

    public Task<PresentationOperationResult<SlideshowConnection>> ConnectPowerPointAsync(
        PresentationHostKind? preferredKind = null,
        CancellationToken cancellationToken = default) =>
        InvokeWithTimeoutAsync(
            () =>
            {
                var app = CreatePowerPointApplication();
                if (!app.IsSuccess)
                {
                    return PresentationOperation.Failure<SlideshowConnection>(app.Error!);
                }

                var slideshowWindow = PickBestSlideshowWindow(app.Value);
                if (slideshowWindow is null)
                {
                    return PresentationOperation.Failure<SlideshowConnection>(PresentationErrors.SlideshowClosed("PowerPoint has no active slideshow window."));
                }

                return PresentationOperation.Success(new SlideshowConnection(
                    slideshowWindow,
                    SafeHwndFromSlideshowWindow(slideshowWindow),
                    PresentationHostKind.PowerPoint,
                    app.Value));
            },
            "connecting to PowerPoint COM",
            cancellationToken);

    public Task<PresentationOperationResult<PresentationState>> GetStateAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default) =>
        InvokeWithTimeoutAsync(
            () => ReadState(connection),
            "reading PowerPoint slideshow state",
            cancellationToken);

    public Task<PresentationOperationResult> NavigateNextAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default) =>
        InvokeCommandAsync(connection, view => InvokeMethod(view, "Next"), "navigating to the next slide", cancellationToken);

    public Task<PresentationOperationResult> NavigatePreviousAsync(
        SlideshowConnection connection,
        CancellationToken cancellationToken = default) =>
        InvokeCommandAsync(connection, view => InvokeMethod(view, "Previous"), "navigating to the previous slide", cancellationToken);

    public Task<PresentationOperationResult> GotoSlideAsync(
        SlideshowConnection connection,
        int slideIndex,
        CancellationToken cancellationToken = default) =>
        InvokeCommandAsync(connection, view => InvokeMethod(view, "GotoSlide", slideIndex), "going to a PowerPoint slide", cancellationToken);

    public Task<PresentationOperationResult> ApplyPenColorAsync(
        SlideshowConnection connection,
        string colorHex,
        CancellationToken cancellationToken = default) =>
        InvokeWithTimeoutAsync(
            () =>
            {
                var view = GetView(connection);
                var pointerColor = GetProperty(view, "PointerColor");
                if (pointerColor is null)
                {
                    return PresentationOperationResult.Failure(PresentationErrors.Unsupported("PowerPoint did not expose PointerColor."));
                }

                var rgb = ToOfficeRgb(PresentationMonitor.NormalizeColorHex(colorHex));
                SetProperty(pointerColor, "RGB", rgb);
                var current = ReadPointerColor(view);
                return string.Equals(current, PresentationMonitor.NormalizeColorHex(colorHex), StringComparison.OrdinalIgnoreCase)
                    ? PresentationOperationResult.Success()
                    : PresentationOperationResult.Failure(PresentationErrors.CommandRejected("PowerPoint did not accept the requested pen color.", current));
            },
            "applying PowerPoint pen color",
            cancellationToken);

    public Task<PresentationOperationResult> ApplyPenColorViaPaletteAsync(
        SlideshowConnection connection,
        string colorHex,
        ISlideshowWindowAdapter windowAdapter,
        CancellationToken cancellationToken = default) =>
        InvokeWithTimeoutAsync(
            async () =>
            {
                var normalized = PresentationMonitor.NormalizeColorHex(colorHex);
                if (!PenColorPalette.TryGetValue(normalized, out var command))
                {
                    return PresentationOperationResult.Failure(PresentationErrors.Unsupported("The requested pen color is not in the legacy PowerPoint palette grid.", normalized));
                }

                var hwnd = connection.Hwnd != 0 ? connection.Hwnd : SafeHwndFromSlideshowWindow(connection.SlideshowWindow);
                if (hwnd == 0)
                {
                    return PresentationOperationResult.Failure(PresentationErrors.SlideshowClosed());
                }

                _ = await windowAdapter.FocusAsync(hwnd, cancellationToken).ConfigureAwait(false);
                _ = ExecuteMso(connection.Application, command.ExecuteMsoCommandId);
                _ = await windowAdapter.PostCtrlShortcutAsync(hwnd, command.ShortcutKey, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(60), cancellationToken).ConfigureAwait(false);
                if (!ExecuteMso(connection.Application, "InkColorPicker"))
                {
                    return PresentationOperationResult.Failure(PresentationErrors.Unsupported("PowerPoint did not expose the InkColorPicker command."));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(120), cancellationToken).ConfigureAwait(false);
                await RepeatKeyAsync(windowAdapter, hwnd, PresentationVirtualKey.Up, command.UpCount, cancellationToken).ConfigureAwait(false);
                await RepeatKeyAsync(windowAdapter, hwnd, PresentationVirtualKey.Left, command.LeftCount, cancellationToken).ConfigureAwait(false);
                await RepeatKeyAsync(windowAdapter, hwnd, PresentationVirtualKey.Down, command.DownCount, cancellationToken).ConfigureAwait(false);
                await RepeatKeyAsync(windowAdapter, hwnd, PresentationVirtualKey.Right, command.RightCount, cancellationToken).ConfigureAwait(false);
                var confirmed = await windowAdapter.PostKeyAsync(hwnd, command.ConfirmKey, cancellationToken).ConfigureAwait(false);
                if (!confirmed.IsSuccess)
                {
                    return confirmed;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken).ConfigureAwait(false);
                return PresentationOperationResult.Success();
            },
            "applying PowerPoint pen color through the palette fallback",
            cancellationToken);

    public Task<PresentationOperationResult> SetPointerTypeAsync(
        SlideshowConnection connection,
        PresentationPointerType pointerType,
        CancellationToken cancellationToken = default) =>
        InvokeWithTimeoutAsync(
            () =>
            {
                var value = ToPowerPointPointerType(pointerType);
                if (value == 0)
                {
                    return PresentationOperationResult.Failure(PresentationErrors.Unsupported("Unknown PowerPoint pointer type."));
                }

                var view = GetView(connection);
                if (pointerType is PresentationPointerType.Pen)
                {
                    SetProperty(view, "PointerType", 1);
                }

                SetProperty(view, "PointerType", value);
                return PresentationOperationResult.Success();
            },
            "setting PowerPoint pointer type",
            cancellationToken);

    private static async Task RepeatKeyAsync(
        ISlideshowWindowAdapter windowAdapter,
        nint hwnd,
        PresentationVirtualKey virtualKey,
        int count,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < count; index++)
        {
            var result = await windowAdapter.PostKeyAsync(hwnd, virtualKey, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task<PresentationOperationResult> InvokeCommandAsync(
        SlideshowConnection connection,
        Action<object> command,
        string context,
        CancellationToken cancellationToken) =>
        InvokeWithTimeoutAsync(
            () =>
            {
                var view = GetView(connection);
                command(view);
                return PresentationOperationResult.Success();
            },
            context,
            cancellationToken);

    private static PresentationOperationResult<PresentationState> ReadState(SlideshowConnection connection)
    {
        var view = GetView(connection);
        var state = ReadIntProperty(view, "State", 1);
        if (state is not (1 or 2))
        {
            return PresentationOperation.Failure<PresentationState>(PresentationErrors.SlideshowClosed(), PresentationState.Empty with { HostKind = PresentationHostKind.PowerPoint });
        }

        var current = ReadIntProperty(view, "CurrentShowPosition", 0);
        if (current <= 0)
        {
            var slide = GetProperty(view, "Slide");
            current = slide is null ? 0 : ReadIntProperty(slide, "SlideIndex", 0);
        }

        var presentation = GetPresentation(connection, view);
        var slides = presentation is null ? null : GetProperty(presentation, "Slides");
        var total = slides is null ? 0 : ReadIntProperty(slides, "Count", 0);
        var isReadOnly = presentation is not null && ReadBoolProperty(presentation, "ReadOnly", false);
        var isProtectedView = ReadProtectedView(connection.Application);
        var pointerType = FromPowerPointPointerType(ReadIntProperty(view, "PointerType", 0));
        var penColor = ReadPointerColor(view);

        return PresentationOperation.Success(new PresentationState(
            current,
            total,
            true,
            pointerType,
            penColor,
            PresentationHostKind.PowerPoint,
            isProtectedView,
            isReadOnly));
    }

    private static PresentationOperationResult<object> CreatePowerPointApplication()
    {
        try
        {
            var type = Type.GetTypeFromProgID(PowerPointProgId, throwOnError: false);
            if (type is null)
            {
                return PresentationOperation.Failure<object>(PresentationErrors.HostUnavailable("PowerPoint.Application is not registered."));
            }

            var instance = Activator.CreateInstance(type);
            return instance is null
                ? PresentationOperation.Failure<object>(PresentationErrors.HostUnavailable("PowerPoint.Application could not be activated."))
                : PresentationOperation.Success(instance);
        }
        catch (Exception exception) when (exception is TypeLoadException or BadImageFormatException or COMException or InvalidOperationException)
        {
            return PresentationOperation.Failure<object>(PresentationErrors.FromException(exception, "activating PowerPoint.Application"));
        }
    }

    private static object? PickBestSlideshowWindow(object app)
    {
        var windows = GetProperty(app, "SlideShowWindows");
        var count = SafeCount(windows);
        if (count <= 0 || windows is null)
        {
            return null;
        }

        for (var index = 1; index <= count; index++)
        {
            var slideshowWindow = InvokeMethod(windows, "Item", index);
            if (slideshowWindow is not null && SafeHwndFromSlideshowWindow(slideshowWindow) != 0)
            {
                return slideshowWindow;
            }
        }

        return InvokeMethod(windows, "Item", 1);
    }

    private static object GetView(SlideshowConnection connection)
    {
        var view = GetProperty(connection.SlideshowWindow, "View");
        return view ?? throw new InvalidOperationException("Slideshow window has no View.");
    }

    private static object? GetPresentation(SlideshowConnection connection, object view)
    {
        var candidates = new[]
        {
            GetProperty(connection.SlideshowWindow, "Presentation"),
            GetProperty(view, "Presentation"),
            connection.Application is null ? null : GetProperty(connection.Application, "ActivePresentation"),
        };
        return candidates.FirstOrDefault(candidate => candidate is not null);
    }

    private static bool ExecuteMso(object? app, string commandId)
    {
        if (app is null)
        {
            return false;
        }

        try
        {
            var commandBars = GetProperty(app, "CommandBars");
            if (commandBars is null)
            {
                return false;
            }

            InvokeMethod(commandBars, "ExecuteMso", commandId);
            return true;
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or MissingMethodException)
        {
            return false;
        }
    }

    private static int SafeCount(object? collection)
    {
        if (collection is null)
        {
            return 0;
        }

        try
        {
            return ReadIntProperty(collection, "Count", 0);
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or MissingMethodException)
        {
            return 0;
        }
    }

    private static nint SafeHwndFromSlideshowWindow(object slideshowWindow)
    {
        try
        {
            var value = GetProperty(slideshowWindow, "HWND");
            return value is null ? 0 : new nint(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or MissingMethodException or FormatException or InvalidCastException)
        {
            return 0;
        }
    }

    private static bool ReadProtectedView(object? app)
    {
        if (app is null)
        {
            return false;
        }

        try
        {
            var protectedViewWindows = GetProperty(app, "ProtectedViewWindows");
            return SafeCount(protectedViewWindows) > 0;
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or MissingMethodException)
        {
            return false;
        }
    }

    private static string ReadPointerColor(object view)
    {
        try
        {
            var pointerColor = GetProperty(view, "PointerColor");
            var value = pointerColor is null ? 0 : ReadIntProperty(pointerColor, "RGB", 0) & 0xFFFFFF;
            var red = value & 0xFF;
            var green = (value >> 8) & 0xFF;
            var blue = (value >> 16) & 0xFF;
            return $"#{red:X2}{green:X2}{blue:X2}";
        }
        catch (Exception exception) when (exception is COMException or TargetInvocationException or MissingMethodException)
        {
            return "#000000";
        }
    }

    private static int ToOfficeRgb(string colorHex)
    {
        var normalized = PresentationMonitor.NormalizeColorHex(colorHex);
        var red = int.Parse(normalized.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var green = int.Parse(normalized.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var blue = int.Parse(normalized.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return red + (green << 8) + (blue << 16);
    }

    private static int ToPowerPointPointerType(PresentationPointerType pointerType) =>
        pointerType switch
        {
            PresentationPointerType.Arrow => 1,
            PresentationPointerType.Pen => 2,
            PresentationPointerType.Highlighter => 3,
            PresentationPointerType.Eraser => 5,
            _ => 0,
        };

    private static PresentationPointerType FromPowerPointPointerType(int pointerType) =>
        pointerType switch
        {
            1 => PresentationPointerType.Arrow,
            2 => PresentationPointerType.Pen,
            3 => PresentationPointerType.Highlighter,
            5 => PresentationPointerType.Eraser,
            _ => PresentationPointerType.Unknown,
        };

    private static int ReadIntProperty(object target, string name, int fallback)
    {
        var value = GetProperty(target, name);
        if (value is null)
        {
            return fallback;
        }

        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return fallback;
        }
    }

    private static bool ReadBoolProperty(object target, string name, bool fallback)
    {
        var value = GetProperty(target, name);
        if (value is null)
        {
            return fallback;
        }

        try
        {
            return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException)
        {
            return fallback;
        }
    }

    private static object? GetProperty(object target, string name) =>
        target.GetType().InvokeMember(
            name,
            BindingFlags.GetProperty,
            binder: null,
            target,
            args: null,
            culture: CultureInfo.InvariantCulture);

    private static void SetProperty(object target, string name, object value) =>
        target.GetType().InvokeMember(
            name,
            BindingFlags.SetProperty,
            binder: null,
            target,
            args: [value],
            culture: CultureInfo.InvariantCulture);

    private static object? InvokeMethod(object target, string name, params object[] args) =>
        target.GetType().InvokeMember(
            name,
            BindingFlags.InvokeMethod,
            binder: null,
            target,
            args,
            culture: CultureInfo.InvariantCulture);

    private static Task<PresentationOperationResult> InvokeWithTimeoutAsync(
        Func<PresentationOperationResult> operation,
        string context,
        CancellationToken cancellationToken)
    {
        return InvokeWithTimeoutCoreAsync(operation, context, cancellationToken);

        static async Task<PresentationOperationResult> InvokeWithTimeoutCoreAsync(
            Func<PresentationOperationResult> operation,
            string context,
            CancellationToken cancellationToken)
        {
        try
        {
            return await RunStaAsync(operation, ComTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PresentationOperationResult.Failure(PresentationErrors.FromException(exception, context));
        }
        }
    }

    private static async Task<PresentationOperationResult<T>> InvokeWithTimeoutAsync<T>(
        Func<PresentationOperationResult<T>> operation,
        string context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunStaAsync(operation, ComTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PresentationOperation.Failure<T>(PresentationErrors.FromException(exception, context));
        }
    }

    private static async Task<PresentationOperationResult> InvokeWithTimeoutAsync(
        Func<Task<PresentationOperationResult>> operation,
        string context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunStaAsync(() => operation().GetAwaiter().GetResult(), ComTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return PresentationOperationResult.Failure(PresentationErrors.FromException(exception, context));
        }
    }

    private static async Task<T> RunStaAsync<T>(Func<T> operation, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                completion.TrySetResult(operation());
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "Luminalium PowerPoint COM STA",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);
        return await completion.Task.WaitAsync(timeoutCancellation.Token).ConfigureAwait(false);
    }

    private static PenColorPaletteCommand Palette(int row, int col) =>
        new(
            "AnnotInkPen",
            PresentationVirtualKey.P,
            UpCount: 3,
            LeftCount: 12,
            DownCount: row,
            RightCount: col,
            ConfirmKey: PresentationVirtualKey.Return);
}
