using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.ViewModels;

/// <summary>
/// 新增 / 编辑账单弹窗的视图模型。
/// </summary>
public sealed partial class TransactionEditViewModel : ViewModelBase
{
    private readonly TransactionService _transactions;
    private readonly IReadOnlyList<CategoryDto> _allCategories;

    [ObservableProperty]
    private TransactionType _type = TransactionType.Expense;

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private AccountDto? _selectedAccount;

    [ObservableProperty]
    private DateTimeOffset? _occurredOn;

    [ObservableProperty]
    private string? _remark;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>构造账单编辑视图模型。</summary>
    /// <param name="transactions">账单用例。</param>
    /// <param name="categories">全部分类（收入 + 支出）。</param>
    /// <param name="accounts">全部账户。</param>
    /// <param name="existing">要编辑的账单；为 <c>null</c> 表示新增。</param>
    public TransactionEditViewModel(
        TransactionService transactions,
        IReadOnlyList<CategoryDto> categories,
        IReadOnlyList<AccountDto> accounts,
        TransactionDto? existing = null)
    {
        _transactions = transactions;
        _allCategories = categories;
        Accounts = accounts;
        Existing = existing;

        var today = DateOnly.FromDateTime(DateTime.Today);
        _occurredOn = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue));

        if (existing is null)
        {
            RefreshCategories();
            _selectedAccount = accounts.FirstOrDefault();
            _selectedCategory = AvailableCategories.FirstOrDefault();
            return;
        }

        _type = existing.Type;
        _amountText = existing.Amount.ToString("0.##", CultureInfo.CurrentCulture);
        _occurredOn = new DateTimeOffset(existing.OccurredOn.ToDateTime(TimeOnly.MinValue));
        _remark = existing.Remark;

        RefreshCategories();
        _selectedAccount = accounts.FirstOrDefault(x => x.Id == existing.AccountId) ?? accounts.FirstOrDefault();
        _selectedCategory = AvailableCategories.FirstOrDefault(x => x.Id == existing.CategoryId)
                            ?? AvailableCategories.FirstOrDefault();
    }

    /// <summary>弹窗关闭请求：参数为 <c>true</c> 表示保存成功。</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>全部账户。</summary>
    public IReadOnlyList<AccountDto> Accounts { get; }

    /// <summary>当前收支类型下可选的分类（支出不显示收入分类）。</summary>
    public ObservableCollection<CategoryDto> AvailableCategories { get; } = [];

    /// <summary>正在编辑的账单，新增时为 <c>null</c>。</summary>
    public TransactionDto? Existing { get; }

    /// <summary>是否为编辑模式。</summary>
    public bool IsEditing => Existing is not null;

    /// <summary>弹窗标题。</summary>
    public string Title => IsEditing ? "编辑账单" : "新增账单";

    /// <summary>是否为支出。</summary>
    public bool IsExpense
    {
        get => Type == TransactionType.Expense;
        set
        {
            if (value)
            {
                ChangeType(TransactionType.Expense);
            }
        }
    }

    /// <summary>是否为收入。</summary>
    public bool IsIncome
    {
        get => Type == TransactionType.Income;
        set
        {
            if (value)
            {
                ChangeType(TransactionType.Income);
            }
        }
    }

    /// <summary>保存。</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        var input = BuildInput();
        if (input is null)
        {
            return;
        }

        try
        {
            if (IsEditing)
            {
                await _transactions.UpdateAsync(input);
            }
            else
            {
                await _transactions.CreateAsync(input);
            }

            RequestClose?.Invoke(this, true);
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存失败：{ex.Message}";
        }
    }

    /// <summary>取消。</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);

    private void ChangeType(TransactionType type)
    {
        if (Type == type)
        {
            return;
        }

        Type = type;
        OnPropertyChanged(nameof(IsExpense));
        OnPropertyChanged(nameof(IsIncome));

        var previousCategoryId = SelectedCategory?.Id;
        RefreshCategories();
        // 类型切换后原分类不再适用，尽量选中同名分类，否则回到第一项
        SelectedCategory = AvailableCategories.FirstOrDefault(x => x.Id == previousCategoryId)
                           ?? AvailableCategories.FirstOrDefault();
    }

    private void RefreshCategories()
    {
        AvailableCategories.Clear();
        foreach (var category in _allCategories.Where(x => x.Type == Type))
        {
            AvailableCategories.Add(category);
        }
    }

    private TransactionInput? BuildInput()
    {
        if (string.IsNullOrWhiteSpace(AmountText))
        {
            ErrorMessage = "请输入金额。";
            return null;
        }

        if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            && !decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            ErrorMessage = "金额格式不正确，只能输入数字。";
            return null;
        }

        if (amount <= 0)
        {
            ErrorMessage = "金额必须大于 0。";
            return null;
        }

        if (SelectedCategory is null)
        {
            ErrorMessage = "请选择分类。";
            return null;
        }

        if (SelectedAccount is null)
        {
            ErrorMessage = "请选择账户。";
            return null;
        }

        if (OccurredOn is not { } occurredOn)
        {
            ErrorMessage = "请选择发生日期。";
            return null;
        }

        return new TransactionInput
        {
            Id = Existing?.Id ?? 0,
            Type = Type,
            Amount = amount,
            CategoryId = SelectedCategory.Id,
            AccountId = SelectedAccount.Id,
            OccurredOn = DateOnly.FromDateTime(occurredOn.DateTime),
            Remark = Remark
        };
    }
}
