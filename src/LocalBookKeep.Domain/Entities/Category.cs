using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Domain.Entities;

/// <summary>
/// 账单分类。分类按收支类型隔离：支出分类与收入分类互不通用。
/// </summary>
public class Category
{
    /// <summary>主键。</summary>
    public int Id { get; set; }

    /// <summary>分类名称，同一收支类型内唯一。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>该分类所属的收支类型。</summary>
    public TransactionType Type { get; set; }

    /// <summary>是否为系统内置分类。内置分类允许改名，但不允许删除。</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>展示排序。</summary>
    public int SortOrder { get; set; }

    /// <summary>关联账单。</summary>
    public ICollection<Transaction> Transactions { get; set; } = [];
}
