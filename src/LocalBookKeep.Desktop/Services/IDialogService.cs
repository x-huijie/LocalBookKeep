using LocalBookKeep.ViewModels;

namespace LocalBookKeep.Services;

/// <summary>
/// 界面弹窗 / 文件选择抽象，避免 ViewModel 直接依赖具体的 Window 类型。
/// </summary>
public interface IDialogService
{
    /// <summary>显示新增 / 编辑账单弹窗，返回是否保存成功。</summary>
    Task<bool> ShowTransactionEditorAsync(TransactionEditViewModel viewModel);

    /// <summary>显示分类管理弹窗。</summary>
    Task ShowCategoryManagerAsync(CategoryManagerViewModel viewModel);

    /// <summary>显示账户管理弹窗。</summary>
    Task ShowAccountManagerAsync(AccountManagerViewModel viewModel);

    /// <summary>显示消息提示。</summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>显示确认框，返回用户是否确认。</summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "确定");

    /// <summary>选择 CSV 保存路径，返回本地文件路径。</summary>
    Task<string?> PickCsvSavePathAsync(string suggestedFileName);

    /// <summary>选择文件夹，返回本地目录路径。</summary>
    Task<string?> PickFolderAsync(string title);

    /// <summary>选择数据库备份文件，返回本地文件路径。</summary>
    Task<string?> PickDatabaseFileAsync();
}
