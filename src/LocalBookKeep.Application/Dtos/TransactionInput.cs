using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Application.Dtos;

/// <summary>
/// 新增 / 修改账单的入参。
/// </summary>
public sealed record TransactionInput
{
    /// <summary>主键，新增时忽略。</summary>
    public int Id { get; init; }

    /// <summary>收支类型。</summary>
    public TransactionType Type { get; init; } = TransactionType.Expense;

    /// <summary>金额（正数）。</summary>
    public decimal Amount { get; init; }

    /// <summary>分类。</summary>
    public int CategoryId { get; init; }

    /// <summary>账户。</summary>
    public int AccountId { get; init; }

    /// <summary>发生日期。</summary>
    public DateOnly OccurredOn { get; init; }

    /// <summary>备注。</summary>
    public string? Remark { get; init; }
}
