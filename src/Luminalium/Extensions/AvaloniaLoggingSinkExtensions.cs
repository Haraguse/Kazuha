using Avalonia;
using Avalonia.Logging;
using Luminalium.Services.Logging;

namespace Luminalium.Extensions;

public static class AvaloniaLoggingSinkExtensions
{
    public static AppBuilder LogToHostSink(this AppBuilder builder, LogEventLevel level = LogEventLevel.Warning)
    {
        Logger.Sink = new AvaloniaLoggingSink(level);
        return builder;
    }
}