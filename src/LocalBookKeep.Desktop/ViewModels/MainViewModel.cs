using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalBookKeep.Application.Abstractions;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Domain.Repositories;
using LocalBookKeep.Services;

namespace LocalBookKeep.ViewModels;

/// <summary>
/// 主窗口视图模型：月度汇总 + 账单流水列表 + 筛选 / 导出 / 备份。
/// </summary>
/// <remarks>
/// 数据库上下文是应用级单例，所有数据库访问都通过 <c>_dbGate</c> 串行化，
/// 避免快速输入搜索关键字时产生并发访问。
/// </remarks>
public sealed partial class MainViewModel : ViewModelBase
{
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(300);

    private readonly TransactionService _transactions;
    private readonly CategoryService _categories;
    private readonly AccountService _accounts;
    private readonly SummaryService _summary;
    private readonly ITransactionCsvExporter _csvExporter;
    private readonly IDatabaseBackupService _backup;
    private readonly IDialogService _dialogs;
    private readonly SemaphoreSlim _dbGate = new(1, 1);
    private readonly DispatcherTimer _searchTimer;

    private string _searchKeyword = string.Empty;
    private MonthOption? _selectedMonth;
    private int _refreshToken;
    private bool _isInitialized;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _periodTitle = string.Empty;

    [ObservableProperty]
    private string _totalIncomeText = "0.00";

    [ObservableProperty]
    private string _totalExpenseText = "0.00";

    [ObservableProperty]
    private string _balanceText = "0.00";

    [ObservableProperty]
    private bool _isBalanceNegative;

    [ObservableProperty]
    private bool _hasTransactions;

    /// <summary>构造主窗口视图模型。</summary>
    public MainViewModel(
        TransactionService transactions,
        CategoryService categories,
        AccountService accounts,
        SummaryService summary,
        ITransactionCsvExporter csvExporter,
        IDatabaseBackupService backup,
        IDialogService dialogs)
    {
        _transactions = transactions;
        _categories = categories;
        _accounts = accounts;
        _summary = summary;
        _csvExporter = csvExporter;
        _backup = backup;
        _dialogs = dialogs;
        DatabasePath = backup.DatabasePath;

        _searchTimer = new DispatcherTimer { Interval = SearchDebounce };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            _ = RunSerializedAsync(LoadAsync);
        };
    }

    /// <summary>账单流水列表。</summary>
    public ObservableCollection<TransactionListItemViewModel> Transactions { get; } = [];

    /// <summary>月份筛选项。</summary>
    public ObservableCollection<MonthOption> Months { get; } = [];

    /// <summary>当前数据库文件位置。</summary>
    public string DatabasePath { get; }

    /// <summary>备注搜索关键字（输入后自动刷新，带防抖）。</summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (!SetProperty(ref _searchKeyword, value))
            {
                return;
            }

            _searchTimer.Stop();
            _searchTimer.Start();
        }
    }

    /// <summary>当前筛选月份。</summary>
    public MonthOption? SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value) && _isInitialized)
            {
                _ = RunSerializedAsync(LoadAsync);
            }
        }
    }

    /// <summary>初始化：加载月份列表并刷新数据。</summary>
    public async Task InitializeAsync()
    {
        await RunSerializedAsync(async () =>
        {
            await LoadMonthsAsync();
            _isInitialized = true;
            await LoadAsync();
        });
    }

    /// <summary>刷新（工具栏按钮）。</summary>
    [RelayCommand]
    private Task Reload() => RunSerializedAsync(async () =>
    {
        await LoadMonthsAsync();
        await LoadAsync();
    });

    /// <summary>新增账单。</summary>
    [RelayCommand]
    private Task AddTransaction() => RunSerializedAsync(() => ShowEditorAsync(null));

    /// <summary>编辑账单。</summary>
    [RelayCommand]
    private Task EditTransaction(TransactionListItemViewModel? item) => RunSerializedAsync(() => ShowEditorAsync(item));

    /// <summary>删除账单。</summary>
    [RelayCommand]
    private Task DeleteTransaction(TransactionListItemViewModel? item) => RunSerializedAsync(async () =>
    {
        if (item is null)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "删除账单",
            $"确定要删除 {item.DateText} 的{item.TypeText} {item.Dto.Amount:N2} 元（{item.CategoryName}）吗？\n删除后账户余额会自动恢复。",
            "删除");

        if (!confirmed)
        {
            return;
        }

        await _transactions.DeleteAsync(item.Id);
        await LoadMonthsAsync();
        await LoadAsync();
    });

    /// <summary>导出当前列表为 CSV。</summary>
    [RelayCommand]
    private Task ExportCsv() => RunSerializedAsync(async () =>
    {
        if (Transactions.Count == 0)
        {
            await _dialogs.ShowMessageAsync("提示", "当前列表没有可导出的账单。");
            return;
        }

        var filePath = await _dialogs.PickCsvSavePathAsync($"账单流水_{DateTime.Now:yyyyMMdd}.csv");
        if (filePath is null)
        {
            return;
        }

        var count = await _csvExporter.ExportAsync([.. Transactions.Select(x => x.Dto)], filePath);
        await _dialogs.ShowMessageAsync("导出完成", $"已导出 {count} 条账单：\n{filePath}\n\n可直接用 Excel 打开。");
    });

    /// <summary>备份数据库。</summary>
    [RelayCommand]
    private Task BackupDatabase() => RunSerializedAsync(async () =>
    {
        var directory = await _dialogs.PickFolderAsync("选择数据库备份目录");
        if (directory is null)
        {
            return;
        }

        var filePath = await _backup.BackupAsync(directory);
        await _dialogs.ShowMessageAsync("备份完成", $"数据库已备份到：\n{filePath}");
    });

    /// <summary>恢复数据库备份。</summary>
    [RelayCommand]
    private Task RestoreDatabase() => RunSerializedAsync(async () =>
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "恢复备份",
            "恢复会用所选备份文件覆盖当前数据库，现有账单、分类、账户都会丢失，且无法撤销。\n\n确定要继续吗？",
            "继续恢复");

        if (!confirmed)
        {
            return;
        }

        var filePath = await _dialogs.PickDatabaseFileAsync();
        if (filePath is null)
        {
            return;
        }

        await _backup.RestoreAsync(filePath);
        await LoadMonthsAsync();
        await LoadAsync();
        await _dialogs.ShowMessageAsync("恢复完成", "数据库已恢复。");
    });

    /// <summary>分类管理。</summary>
    [RelayCommand]
    private Task ManageCategories() => RunSerializedAsync(async () =>
    {
        var viewModel = new CategoryManagerViewModel(_categories);
        await viewModel.InitializeAsync();
        await _dialogs.ShowCategoryManagerAsync(viewModel);

        // 分类改名后列表里的名称需要同步
        await LoadAsync();
    });

    /// <summary>账户管理。</summary>
    [RelayCommand]
    private Task ManageAccounts() => RunSerializedAsync(async () =>
    {
        var viewModel = new AccountManagerViewModel(_accounts);
        await viewModel.InitializeAsync();
        await _dialogs.ShowAccountManagerAsync(viewModel);
    });

    private async Task ShowEditorAsync(TransactionListItemViewModel? item)
    {
        var categories = await _categories.GetAllAsync();
        var accounts = await _accounts.GetAllAsync();

        if (accounts.Count == 0)
        {
            await _dialogs.ShowMessageAsync("提示", "还没有可用账户，请先在「账户管理」中新增账户。");
            return;
        }

        if (categories.Count == 0)
        {
            await _dialogs.ShowMessageAsync("提示", "还没有可用分类，请先在「分类管理」中新增分类。");
            return;
        }

        var editor = new TransactionEditViewModel(_transactions, categories, accounts, item?.Dto);
        if (!await _dialogs.ShowTransactionEditorAsync(editor))
        {
            return;
        }

        await LoadMonthsAsync();
        await LoadAsync();
    }

    private async Task LoadMonthsAsync()
    {
        var months = await _summary.GetAvailableMonthsAsync();
        var currentSelection = SelectedMonth?.Value;

        Months.Clear();
        Months.Add(MonthOption.All);
        foreach (var month in months)
        {
            Months.Add(MonthOption.From(month));
        }

        var target = currentSelection is null
            ? Months.FirstOrDefault(x => x.Value == SummaryService.CurrentMonth())
            : Months.FirstOrDefault(x => x.Value == currentSelection);

        // 直接写字段，避免触发 SelectedMonth 的刷新逻辑
        _selectedMonth = target ?? Months.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedMonth));
    }

    private async Task LoadAsync()
    {
        var token = ++_refreshToken;
        var month = SelectedMonth?.Value;
        var keyword = SearchKeyword;

        var query = month is null
            ? new TransactionQuery { RemarkKeyword = keyword }
            : new TransactionQuery
            {
                From = new DateOnly(month.Value.Year, month.Value.Month, 1),
                To = new DateOnly(month.Value.Year, month.Value.Month,
                    DateTime.DaysInMonth(month.Value.Year, month.Value.Month)),
                RemarkKeyword = keyword
            };

        var items = await _transactions.QueryAsync(query);
        var summary = await _summary.GetSummaryAsync(month);

        // 只应用最后一次发起的结果，避免旧结果覆盖新结果
        if (token != _refreshToken)
        {
            return;
        }

        Transactions.Clear();
        foreach (var item in items)
        {
            Transactions.Add(new TransactionListItemViewModel(item));
        }

        HasTransactions = Transactions.Count > 0;
        TotalIncomeText = summary.TotalIncome.ToString("N2");
        TotalExpenseText = summary.TotalExpense.ToString("N2");
        BalanceText = summary.Balance.ToString("N2");
        IsBalanceNegative = summary.Balance < 0;
        PeriodTitle = month is null ? "全部月份概览" : $"{month.Value.Year} 年 {month.Value.Month} 月概览";

        var keywordTip = string.IsNullOrWhiteSpace(keyword) ? string.Empty : $"，备注包含「{keyword.Trim()}」";
        StatusText = $"共 {Transactions.Count} 条记录{keywordTip}　|　数据库：{DatabasePath}";
    }

    /// <summary>串行执行数据库操作，并统一把异常转成用户可读的提示。</summary>
    private async Task RunSerializedAsync(Func<Task> action)
    {
        await _dbGate.WaitAsync();
        try
        {
            await action();
        }
        catch (ValidationException ex)
        {
            await _dialogs.ShowMessageAsync("提示", ex.Message);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowMessageAsync("操作失败", ex.Message);
        }
        finally
        {
            _dbGate.Release();
        }
    }
}
