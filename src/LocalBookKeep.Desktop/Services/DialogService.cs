using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LocalBookKeep.ViewModels;
using LocalBookKeep.Views;

namespace LocalBookKeep.Services;

/// <summary>
/// 基于 Avalonia 原生窗口与存储选择器的弹窗服务。
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <summary>主窗口，作为所有模态弹窗的宿主。</summary>
    public Window? Owner { get; set; }

    private Window OwnerWindow => Owner ?? throw new InvalidOperationException("主窗口尚未初始化。");

    /// <inheritdoc />
    public Task<bool> ShowTransactionEditorAsync(TransactionEditViewModel viewModel)
    {
        var window = new TransactionEditWindow { DataContext = viewModel };
        return ShowModalAsync(window, viewModel);
    }

    /// <inheritdoc />
    public async Task ShowCategoryManagerAsync(CategoryManagerViewModel viewModel)
    {
        var window = new CategoryManagerWindow { DataContext = viewModel };
        await ShowModalAsync(window, viewModel);
    }

    /// <inheritdoc />
    public async Task ShowAccountManagerAsync(AccountManagerViewModel viewModel)
    {
        var window = new AccountManagerWindow { DataContext = viewModel };
        await ShowModalAsync(window, viewModel);
    }

    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message)
    {
        var window = new MessageWindow(title, message, "确定", showCancel: false);
        await window.ShowDialog(OwnerWindow);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "确定")
    {
        var window = new MessageWindow(title, message, confirmText, showCancel: true);
        return window.ShowDialog<bool>(OwnerWindow);
    }

    /// <inheritdoc />
    public async Task<string?> PickCsvSavePathAsync(string suggestedFileName)
    {
        var file = await OwnerWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出账单为 CSV",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "csv",
            ShowOverwritePrompt = true,
            FileTypeChoices =
            [
                new FilePickerFileType("CSV 文件") { Patterns = ["*.csv"] }
            ]
        });

        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string title)
    {
        var folders = await OwnerWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> PickDatabaseFileAsync()
    {
        var files = await OwnerWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择数据库备份文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("SQLite 数据库") { Patterns = ["*.db"] },
                new FilePickerFileType("所有文件") { Patterns = ["*"] }
            ]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<bool> ShowModalAsync(Window window, ViewModelBase viewModel)
    {
        var result = false;

        void OnRequestClose(object? sender, bool saved)
        {
            result = saved;
            window.Close(saved);
        }

        switch (viewModel)
        {
            case TransactionEditViewModel editor:
                editor.RequestClose += OnRequestClose;
                break;
            case CategoryManagerViewModel categoryManager:
                categoryManager.RequestClose += OnRequestClose;
                break;
            case AccountManagerViewModel accountManager:
                accountManager.RequestClose += OnRequestClose;
                break;
        }

        try
        {
            await window.ShowDialog(OwnerWindow);
        }
        finally
        {
            switch (viewModel)
            {
                case TransactionEditViewModel editor:
                    editor.RequestClose -= OnRequestClose;
                    break;
                case CategoryManagerViewModel categoryManager:
                    categoryManager.RequestClose -= OnRequestClose;
                    break;
                case AccountManagerViewModel accountManager:
                    accountManager.RequestClose -= OnRequestClose;
                    break;
            }
        }

        return result;
    }
}
