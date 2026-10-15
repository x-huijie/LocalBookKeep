using Avalonia.Controls;
using Avalonia.Input;
using LocalBookKeep.ViewModels;

namespace LocalBookKeep.Views;

/// <summary>
/// 主窗口。
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>构造主窗口。</summary>
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    /// <summary>双击账单行进入编辑。</summary>
    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: TransactionListItemViewModel item }
            && DataContext is MainViewModel viewModel)
        {
            viewModel.EditTransactionCommand.Execute(item);
        }
    }
}
