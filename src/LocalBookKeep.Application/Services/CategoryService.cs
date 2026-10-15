using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;

namespace LocalBookKeep.Application.Services;

/// <summary>
/// 分类用例：查询、新增、改名、删除。
/// </summary>
public sealed class CategoryService
{
    /// <summary>分类名称长度上限。</summary>
    public const int MaxNameLength = 20;

    private readonly ICategoryRepository _categories;

    /// <summary>构造分类服务。</summary>
    public CategoryService(ICategoryRepository categories)
    {
        _categories = categories;
    }

    /// <summary>查询全部分类（按类型、排序号排列）。</summary>
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _categories.GetAllAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    /// <summary>按收支类型查询分类，收入分类与支出分类互相隔离。</summary>
    public async Task<IReadOnlyList<CategoryDto>> GetByTypeAsync(
        TransactionType type,
        CancellationToken cancellationToken = default)
    {
        var list = await _categories.GetByTypeAsync(type, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    /// <summary>新增自定义分类。</summary>
    public async Task<int> CreateAsync(
        string name,
        TransactionType type,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(name);
        await EnsureNameAvailableAsync(normalized, type, null, cancellationToken);

        var existing = await _categories.GetByTypeAsync(type, cancellationToken);
        var entity = new Category
        {
            Name = normalized,
            Type = type,
            IsBuiltIn = false,
            SortOrder = existing.Count == 0 ? 1 : existing.Max(c => c.SortOrder) + 1
        };

        await _categories.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    /// <summary>修改分类名称。</summary>
    public async Task RenameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(name);
        var entity = await _categories.GetByIdAsync(id, cancellationToken)
                     ?? throw new ValidationException("分类不存在，可能已被删除。");

        if (entity.Name == normalized)
        {
            return;
        }

        await EnsureNameAvailableAsync(normalized, entity.Type, id, cancellationToken);
        entity.Name = normalized;
        await _categories.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>删除自定义分类；内置分类或已被账单引用的分类不允许删除。</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _categories.GetByIdAsync(id, cancellationToken)
                     ?? throw new ValidationException("分类不存在，可能已被删除。");

        if (entity.IsBuiltIn)
        {
            throw new ValidationException($"「{entity.Name}」是内置分类，不能删除，可以修改名称。");
        }

        var used = await _categories.CountTransactionsAsync(id, cancellationToken);
        if (used > 0)
        {
            throw new ValidationException($"「{entity.Name}」已被 {used} 条账单使用，请先修改这些账单的分类。");
        }

        await _categories.DeleteAsync(id, cancellationToken);
    }

    private static string NormalizeName(string? name)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ValidationException("分类名称不能为空。");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ValidationException($"分类名称最多 {MaxNameLength} 个字符。");
        }

        return normalized;
    }

    private async Task EnsureNameAvailableAsync(
        string name,
        TransactionType type,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (await _categories.ExistsAsync(name, type, excludeId, cancellationToken))
        {
            throw new ValidationException($"已存在{type.ToDisplayName()}分类「{name}」。");
        }
    }

    private static CategoryDto ToDto(Category entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Type = entity.Type,
        IsBuiltIn = entity.IsBuiltIn,
        SortOrder = entity.SortOrder
    };
}
