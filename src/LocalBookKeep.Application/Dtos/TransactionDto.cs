using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Application.Dtos;

/// <summary>
/// 账单展示模型。
/// </summary>
public sealed record TransactionDto
{
    /// <summary>主键。</summary>
    public int Id { get; init; }

    /// <summary>收支类型。</summary>
    public TransactionType Type { get; init; }

    /// <summary>金额（正数）。</summary>
    public decimal Amount { get; init; }

    /// <summary>发生日期。</summary>
    public DateOnly OccurredOn { get; init; }

    /// <summary>备注。</summary>
    public string? Remark { get; init; }

    /// <summary>分类主键。</summary>
    public int CategoryId { get; init; }

    /// <summary>分类名称。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>账户主键。</summary>
    public int AccountId { get; init; }

    /// <summary>账户名称。</summary>
    public string AccountName { get; init; } = string.Empty;
}
