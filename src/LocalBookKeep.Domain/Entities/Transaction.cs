using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Domain.Entities;

/// <summary>
/// 一条收支流水（账单）。
/// </summary>
public class Transaction
{
    /// <summary>主键。</summary>
    public int Id { get; set; }

    /// <summary>收入 / 支出。</summary>
    public TransactionType Type { get; set; }

    /// <summary>金额，始终为正数，方向由 <see cref="Type"/> 决定。</summary>
    public decimal Amount { get; set; }

    /// <summary>发生日期（只到日，支持选择过去日期）。</summary>
    public DateOnly OccurredOn { get; set; }

    /// <summary>备注，可为空。</summary>
    public string? Remark { get; set; }

    /// <summary>分类外键。</summary>
    public int CategoryId { get; set; }

    /// <summary>分类导航属性。</summary>
    public Category? Category { get; set; }

    /// <summary>账户外键。</summary>
    public int AccountId { get; set; }

    /// <summary>账户导航属性。</summary>
    public Account? Account { get; set; }

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最近修改时间。</summary>
    public DateTime UpdatedAt { get; set; }
}
