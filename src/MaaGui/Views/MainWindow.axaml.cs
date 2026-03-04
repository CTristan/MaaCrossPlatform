using System.Linq;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using MaaGui.ViewModels;

namespace MaaGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        var navView = this.FindControl<NavigationView>("MainNavigationView");
        if (navView != null)
        {
            navView.SelectionChanged += (s, e) =>
            {
                if (e.IsSettingsSelected)
                {
                    viewModel.SelectedTab = "Settings";
                }
                else if (e.SelectedItem is NavigationViewItem item && item.Tag is string tag)
                {
                    viewModel.SelectedTab = tag;
                }
            };

            // Set default selection
            navView.SelectedItem = navView.MenuItems.Cast<NavigationViewItem>().FirstOrDefault();
        }
    }
}
