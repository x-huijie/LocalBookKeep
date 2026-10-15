using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;

namespace LocalBookKeep.ViewModels;

/// <summary>
/// 账户管理弹窗的视图模型。
/// </summary>
public sealed partial class AccountManagerViewModel : ViewModelBase
{
    private readonly AccountService _accounts;

    [ObservableProperty]
    private AccountDto? _selectedAccount;

    [ObservableProperty]
    private string _nameInput = string.Empty;

    [ObservableProperty]
    private string _initialBalanceInput = "0";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _totalBalanceText = "0.00";

    /// <summary>构造账户管理视图模型。</summary>
    public AccountManagerViewModel(AccountService accounts)
    {
        _accounts = accounts;
    }

    /// <summary>弹窗关闭请求。</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>账户列表（含当前余额）。</summary>
    public ObservableCollection<AccountDto> Accounts { get; } = [];

    /// <summary>加载账户列表。</summary>
    public async Task InitializeAsync() => await LoadAsync();

    /// <summary>新增账户。</summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        ErrorMessage = null;
        if (!TryParseInitialBalance(out var initialBalance))
        {
            return;
        }

        try
        {
            await _accounts.CreateAsync(NameInput, initialBalance);
            NameInput = string.Empty;
            InitialBalanceInput = "0";
            await LoadAsync();
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>保存选中账户的名称与期初余额。</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedAccount is null)
        {
            ErrorMessage = "请先在列表中选择要修改的账户。";
            return;
        }

        if (!TryParseInitialBalance(out var initialBalance))
        {
            return;
        }

        try
        {
            var id = SelectedAccount.Id;
            await _accounts.UpdateAsync(id, NameInput, initialBalance);
            await LoadAsync();
            SelectedAccount = Accounts.FirstOrDefault(x => x.Id == id);
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>删除选中的自定义账户。</summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        ErrorMessage = null;
        if (SelectedAccount is null)
        {
            ErrorMessage = "请先在列表中选择要删除的账户。";
            return;
        }

        try
        {
            await _accounts.DeleteAsync(SelectedAccount.Id);
            SelectedAccount = null;
            NameInput = string.Empty;
            InitialBalanceInput = "0";
            await LoadAsync();
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>关闭弹窗。</summary>
    [RelayCommand]
    private void Close() => RequestClose?.Invoke(this, true);

    private async Task LoadAsync()
    {
        var list = await _accounts.GetAllAsync();
        Accounts.Clear();
        foreach (var item in list)
        {
            Accounts.Add(item);
        }

        TotalBalanceText = list.Sum(x => x.CurrentBalance).ToString("N2");
    }

    private bool TryParseInitialBalance(out decimal value)
    {
        var text = string.IsNullOrWhiteSpace(InitialBalanceInput) ? "0" : InitialBalanceInput;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        ErrorMessage = "期初余额格式不正确，只能输入数字。";
        value = 0m;
        return false;
    }

    partial void OnSelectedAccountChanged(AccountDto? value)
    {
        NameInput = value?.Name ?? string.Empty;
        InitialBalanceInput = (value?.InitialBalance ?? 0m).ToString("0.##", CultureInfo.CurrentCulture);
    }
}
