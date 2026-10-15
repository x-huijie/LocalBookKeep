using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Application.Dtos;

/// <summary>
/// 分类展示模型。
/// </summary>
public sealed record CategoryDto
{
    /// <summary>主键。</summary>
    public int Id { get; init; }

    /// <summary>名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>收支类型。</summary>
    public TransactionType Type { get; init; }

    /// <summary>是否内置分类。</summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>排序。</summary>
    public int SortOrder { get; init; }

    /// <summary>下拉框显示文本。</summary>
    public string DisplayName => Name;
}
