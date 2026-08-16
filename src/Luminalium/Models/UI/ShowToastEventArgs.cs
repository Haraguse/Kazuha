using Avalonia.Interactivity;
using Luminalium.Controls;

namespace Luminalium.Models.UI;

/// <summary>
///     显示 Toast 事件参数。
/// </summary>
public class ShowToastEventArgs : RoutedEventArgs
{
    internal ShowToastEventArgs(ToastMessage message) : base(AppToastAdorner.ShowToastEvent)
    {
        Message = message;
    }

    /// <summary>
    ///     Toast 事件包含的消息。
    /// </summary>
    public ToastMessage Message { get; }
}