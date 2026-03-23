using DSAapp.Contracts.Services;
using DSAapp.Helpers;
using DSAapp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace DSAapp.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;

        AppTitleBarText.Text = "AppDisplayName".GetLocalized();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        => App.AppTitlebar = AppTitleBarText as UIElement;

    private void NavigationViewControl_DisplayModeChanged(
        NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        // Ajusta el margen del título según el modo de visualización
        double paneLength = sender.CompactPaneLength;
        double multiplier = sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1;

        AppTitleBar.Margin = new Thickness(paneLength * multiplier, 0, 0, 0);
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(
        VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var ka = new KeyboardAccelerator { Key = key };
        if (modifiers.HasValue) ka.Modifiers = modifiers.Value;
        ka.Invoked += OnKeyboardAcceleratorInvoked;
        return ka;
    }

    private static void OnKeyboardAcceleratorInvoked(
        KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var nav = App.GetService<INavigationService>();
        args.Handled = nav.GoBack();
    }
}