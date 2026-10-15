using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.ViewModels;

/// <summary>
/// 分类管理弹窗的视图模型。
/// </summary>
public sealed partial class CategoryManagerViewModel : ViewModelBase
{
    private readonly CategoryService _categories;

    [ObservableProperty]
    private TransactionType _currentType = TransactionType.Expense;

    [ObservableProperty]
    private CategoryDto? _selectedCategory;

    [ObservableProperty]
    private string _nameInput = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>构造分类管理视图模型。</summary>
    public CategoryManagerViewModel(CategoryService categories)
    {
        _categories = categories;
    }

    /// <summary>弹窗关闭请求。</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>当前类型下的分类列表。</summary>
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    /// <summary>是否为支出分类。</summary>
    public bool IsExpense
    {
        get => CurrentType == TransactionType.Expense;
        set
        {
            if (value)
            {
                _ = ChangeTypeAsync(TransactionType.Expense);
            }
        }
    }

    /// <summary>是否为收入分类。</summary>
    public bool IsIncome
    {
        get => CurrentType == TransactionType.Income;
        set
        {
            if (value)
            {
                _ = ChangeTypeAsync(TransactionType.Income);
            }
        }
    }

    /// <summary>加载当前类型的分类。</summary>
    public async Task InitializeAsync() => await LoadAsync();

    /// <summary>新增分类。</summary>
    [RelayCommand]
    private async Task CreateAsync()
    {
        ErrorMessage = null;
        try
        {
            await _categories.CreateAsync(NameInput, CurrentType);
            NameInput = string.Empty;
            await LoadAsync();
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>保存选中分类的新名称。</summary>
    [RelayCommand]
    private async Task RenameAsync()
    {
        ErrorMessage = null;
        if (SelectedCategory is null)
        {
            ErrorMessage = "请先在列表中选择要修改的分类。";
            return;
        }

        try
        {
            var id = SelectedCategory.Id;
            await _categories.RenameAsync(id, NameInput);
            await LoadAsync();
            SelectedCategory = Categories.FirstOrDefault(x => x.Id == id);
            NameInput = SelectedCategory?.Name ?? string.Empty;
        }
        catch (ValidationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>删除选中的自定义分类。</summary>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        ErrorMessage = null;
        if (SelectedCategory is null)
        {
            ErrorMessage = "请先在列表中选择要删除的分类。";
            return;
        }

        try
        {
            await _categories.DeleteAsync(SelectedCategory.Id);
            SelectedCategory = null;
            NameInput = string.Empty;
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

    private async Task ChangeTypeAsync(TransactionType type)
    {
        if (CurrentType == type)
        {
            return;
        }

        CurrentType = type;
        OnPropertyChanged(nameof(IsExpense));
        OnPropertyChanged(nameof(IsIncome));
        ErrorMessage = null;
        NameInput = string.Empty;
        SelectedCategory = null;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var list = await _categories.GetByTypeAsync(CurrentType);
        Categories.Clear();
        foreach (var item in list)
        {
            Categories.Add(item);
        }
    }

    partial void OnSelectedCategoryChanged(CategoryDto? value)
        => NameInput = value?.Name ?? string.Empty;
}
