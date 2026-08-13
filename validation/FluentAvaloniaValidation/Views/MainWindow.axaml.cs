using System;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using FluentAvaloniaValidation.Services;

namespace FluentAvaloniaValidation.Views;

/// <summary>
/// Native FluentAvalonia main window for the Task 2 validation project.
/// Wires the FA object-based navigation contract (IFANavigationPageFactory / NavigateFromObject)
/// through the FANavigationView + FAFrame pair.
/// Mica is applied automatically by FAAppWindow on Windows 11 (FA 3.x exposes no public toggle).
/// </summary>
public partial class MainWindow : FAAppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ValidationFrame.NavigationPageFactory = new ValidationPageFactory();
    }

    private void OnItemInvoked(object? sender, FANavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer?.Tag is string tag)
        {
            ValidationFrame.NavigateFromObject(tag);
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (ValidationNavigation.MenuItems.Count > 0)
        {
            ValidationNavigation.SelectedItem = ValidationNavigation.MenuItems[0];
        }

        ValidationFrame.NavigateFromObject("overview");
    }
}
