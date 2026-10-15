namespace LocalBookKeep.Domain.Entities;

/// <summary>
/// 资金账户（微信、支付宝、银行卡、现金……）。
/// </summary>
/// <remarks>
/// 账户当前余额不落库，统一由 <c>期初余额 + Σ收入 - Σ支出</c> 实时推导，
/// 这样修改、删除账单时余额自动保持一致，不会出现对不上账的情况。
/// </remarks>
public class Account
{
    /// <summary>主键。</summary>
    public int Id { get; set; }

    /// <summary>账户名称，全局唯一。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>期初余额（建立账户时的金额）。</summary>
    public decimal InitialBalance { get; set; }

    /// <summary>是否为系统内置账户。内置账户允许改名，但不允许删除。</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>展示排序。</summary>
    public int SortOrder { get; set; }

    /// <summary>关联账单。</summary>
    public ICollection<Transaction> Transactions { get; set; } = [];
}
