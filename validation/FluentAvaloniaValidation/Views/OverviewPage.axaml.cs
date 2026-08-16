using System;
using System.Text;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            DumpComboBoxLayout("默认32 (ComboDefault32)", ComboDefault32);
            DumpComboBoxLayout("触摸40 (ComboTouch40)", ComboTouch40);
            DumpComboBoxLayout("应用样式 (ComboAppStyle)", ComboAppStyle);
            DumpComboBoxLayout("字符串40 (ComboString40)", ComboString40);
        };
        timer.Start();
    }

    private static void DumpComboBoxLayout(string label, ComboBox cb)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== {label} ===");
        sb.AppendLine($"ComboBox Bounds={cb.Bounds} Desired={cb.DesiredSize} Padding={cb.Padding} VCA={cb.VerticalContentAlignment}");
        foreach (var v in cb.GetVisualDescendants())
        {
            if (v is ContentControl cc && cc.Name == "ContentPresenter")
            {
                sb.AppendLine($"  ContentControl Bounds={cc.Bounds} VA={cc.VerticalAlignment} VCA={cc.VerticalContentAlignment} HCA={cc.HorizontalContentAlignment} Margin={cc.Margin}");
            }
            if (v is TextBlock tb)
            {
                sb.AppendLine($"  TextBlock '{tb.Text}' Bounds={tb.Bounds} VA={tb.VerticalAlignment}");
            }
        }
        System.IO.File.AppendAllText(@"C:\Users\Evan Evan\Documents\Luminalium\validation\_combodump.txt", sb.ToString() + "\n");
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
