using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Domain.Repositories;

/// <summary>
/// 分类仓储契约。
/// </summary>
public interface ICategoryRepository
{
    /// <summary>查询全部分类。</summary>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>按收支类型查询分类。</summary>
    Task<IReadOnlyList<Category>> GetByTypeAsync(TransactionType type, CancellationToken cancellationToken = default);

    /// <summary>按主键查询分类。</summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>判断同一收支类型下是否已存在同名分类。</summary>
    Task<bool> ExistsAsync(string name, TransactionType type, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>统计该分类下已使用的账单数量。</summary>
    Task<int> CountTransactionsAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>新增分类。</summary>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>更新分类。</summary>
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>删除分类。</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
