using Avalonia.Controls;
using Sample.ViewModels;

namespace Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
