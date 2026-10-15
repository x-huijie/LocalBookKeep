namespace LocalBookKeep.Application.Dtos;

/// <summary>
/// 账户展示模型。
/// </summary>
public sealed record AccountDto
{
    /// <summary>主键。</summary>
    public int Id { get; init; }

    /// <summary>名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>期初余额。</summary>
    public decimal InitialBalance { get; init; }

    /// <summary>当前余额 = 期初余额 + 收入合计 - 支出合计。</summary>
    public decimal CurrentBalance { get; init; }

    /// <summary>是否内置账户。</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>排序。</summary>
    public int SortOrder { get; init; }

    /// <summary>已关联的账单数量。</summary>
    public int TransactionCount { get; init; }

    /// <summary>下拉框显示文本。</summary>
    public string DisplayName => Name;
}
