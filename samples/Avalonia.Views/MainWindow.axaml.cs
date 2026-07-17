using Avalonia.Controls;
using Sample.ViewModels;

namespace Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Opened += async (_, _) => await _viewModel.OnLoadedAsync();
    }
}
