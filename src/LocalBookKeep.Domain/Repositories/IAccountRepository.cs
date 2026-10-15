using LocalBookKeep.Domain.Entities;

namespace LocalBookKeep.Domain.Repositories;

/// <summary>
/// 账户仓储契约。
/// </summary>
public interface IAccountRepository
{
    /// <summary>查询全部账户。</summary>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>按主键查询账户。</summary>
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>判断账户名称是否已存在。</summary>
    Task<bool> ExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>统计该账户下已使用的账单数量。</summary>
    Task<int> CountTransactionsAsync(int accountId, CancellationToken cancellationToken = default);

    /// <summary>新增账户。</summary>
    Task AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>更新账户。</summary>
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>删除账户。</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
