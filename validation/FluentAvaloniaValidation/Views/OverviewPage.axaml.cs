using System;
using System.Text;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FluentAvaloniaValidation.Interop;

namespace FluentAvaloniaValidation.Views;

public partial class OverviewPage : UserControl
{
    private bool _initialized;

    public OverviewPage()
    {
        InitializeComponent();
        OpenValidationDialog.Click += OnOpenValidationDialogClicked;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_initialized) return;
        _initialized = true;
        EnvText.Text = BuildEnvironmentText();
    }

    private static string BuildEnvironmentText()
    {
        var os = WindowsVersionInfo.Get();
        var sb = new StringBuilder();
        sb.AppendLine($"操作系统 (RtlGetVersion)  : {os}   |  >= 1809 (10.0.17763): {os.IsAtLeast(10, 0, 17763)}");
        sb.AppendLine($"Environment.OSVersion    : {Environment.OSVersion.Version}");
        sb.AppendLine($".NET Runtime             : {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"TargetFramework (TFM)    : net10.0-windows10.0.17763.0 (Windows 10 build 17763 = 1809)");
        sb.AppendLine($"Avalonia                 : 12.1.1 (pinned)");
        sb.AppendLine($"FluentAvaloniaUI         : 3.0.2 (pinned)");
        sb.AppendLine($"CommunityToolkit.Mvvm    : 8.4.2 (pinned)");
        return sb.ToString();
    }

    private async void OnOpenValidationDialogClicked(object? sender, RoutedEventArgs e)
    {
        var dialog = new FAContentDialog
        {
            Title = "验证对话框 · Validation Dialog",
            Content = "FluentAvalonia validation — ContentDialog 异步对话框验证通过。\n\n中文内容渲染正常，等待您确认。",
            PrimaryButtonText = "确定",
            CloseButtonText = "关闭",
            DefaultButton = FAContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        DialogResultText.Text = $"对话框结果 · DialogResult: {result}";
    }
}
