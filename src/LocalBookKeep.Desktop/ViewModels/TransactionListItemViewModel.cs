using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.ViewModels;

/// <summary>
/// 账单列表中的一行。
/// </summary>
public sealed class TransactionListItemViewModel
{
    private static readonly string[] WeekNames = ["日", "一", "二", "三", "四", "五", "六"];

    /// <summary>包装一条账单。</summary>
    public TransactionListItemViewModel(TransactionDto dto)
    {
        Dto = dto;
    }

    /// <summary>原始账单数据。</summary>
    public TransactionDto Dto { get; }

    /// <summary>主键。</summary>
    public int Id => Dto.Id;

    /// <summary>发生日期。</summary>
    public string DateText => $"{Dto.OccurredOn:yyyy-MM-dd} 周{WeekNames[(int)Dto.OccurredOn.DayOfWeek]}";

    /// <summary>收支类型文本。</summary>
    public string TypeText => Dto.Type.ToDisplayName();

    /// <summary>是否为收入。</summary>
    public bool IsIncome => Dto.Type == TransactionType.Income;

    /// <summary>是否为支出。</summary>
    public bool IsExpense => Dto.Type == TransactionType.Expense;

    /// <summary>带正负号的金额文本。</summary>
    public string AmountText => (IsIncome ? "+" : "-") + Dto.Amount.ToString("N2");

    /// <summary>分类名称。</summary>
    public string CategoryName => Dto.CategoryName;

    /// <summary>账户名称。</summary>
    public string AccountName => Dto.AccountName;

    /// <summary>备注（空备注显示占位符）。</summary>
    public string RemarkText => string.IsNullOrWhiteSpace(Dto.Remark) ? "—" : Dto.Remark!;
}
