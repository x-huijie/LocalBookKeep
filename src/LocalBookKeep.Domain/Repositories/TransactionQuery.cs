using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Domain.Repositories;

/// <summary>
/// 账单查询条件（V1：月份 + 备注关键字；V2 可继续扩展分类/账户筛选）。
/// </summary>
public sealed class TransactionQuery
{
    /// <summary>起始日期（含）。</summary>
    public DateOnly? From { get; init; }

    /// <summary>结束日期（含）。</summary>
    public DateOnly? To { get; init; }

    /// <summary>备注关键字，模糊匹配。</summary>
    public string? RemarkKeyword { get; init; }

    /// <summary>分类筛选。</summary>
    public int? CategoryId { get; init; }

    /// <summary>账户筛选。</summary>
    public int? AccountId { get; init; }

    /// <summary>收支类型筛选。</summary>
    public TransactionType? Type { get; init; }

    /// <summary>按月份筛选。</summary>
    public static TransactionQuery ForMonth(DateOnly month) => new()
    {
        From = new DateOnly(month.Year, month.Month, 1),
        To = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month))
    };
}
