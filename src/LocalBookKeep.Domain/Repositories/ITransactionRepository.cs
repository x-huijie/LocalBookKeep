using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Domain.Repositories;

/// <summary>
/// 账单仓储契约。
/// </summary>
public interface ITransactionRepository
{
    /// <summary>按条件查询账单，结果按发生日期倒序（最新在前）。</summary>
    Task<IReadOnlyList<Transaction>> QueryAsync(TransactionQuery query, CancellationToken cancellationToken = default);

    /// <summary>查询全部账单（用于导出）。</summary>
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>按主键查询账单。</summary>
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>新增账单。</summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>更新账单。</summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>删除账单。</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>统计某段时间内指定收支类型的金额合计。</summary>
    Task<decimal> GetTotalAmountAsync(
        TransactionType type,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    /// <summary>获取每个账户的净额（收入为正、支出为负），账户没有流水时不会出现在结果中。</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetAccountNetAmountsAsync(CancellationToken cancellationToken = default);

    /// <summary>获取所有存在账单的月份（每月第一天），用于月份筛选下拉框。</summary>
    Task<IReadOnlyList<DateOnly>> GetDistinctMonthsAsync(CancellationToken cancellationToken = default);
}
