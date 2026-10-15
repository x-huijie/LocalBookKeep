using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Persistence.Repositories;

/// <summary>
/// 基于 EF Core 的账单仓储实现。
/// </summary>
/// <remarks>
/// 金额统计（总计、账户余额）统一把明细取回后在内存中用 <see cref="decimal"/> 汇总，
/// 避免不同数据库对 decimal 聚合的精度差异（SQLite 以文本存储 decimal）。
/// </remarks>
public sealed class TransactionRepository : ITransactionRepository
{
    private readonly BookKeepDbContext _context;

    /// <summary>构造账单仓储。</summary>
    public TransactionRepository(BookKeepDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> QueryAsync(
        TransactionQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = BuildQuery(query);

        return await source
            .OrderByDescending(x => x.OccurredOn)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Transactions
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Account)
            .OrderByDescending(x => x.OccurredOn)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Transactions
            .Include(x => x.Category)
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Transactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _context.Transactions.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetTotalAmountAsync(
        TransactionType type,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions.AsNoTracking().Where(x => x.Type == type);

        if (from is { } start)
        {
            query = query.Where(x => x.OccurredOn >= start);
        }

        if (to is { } end)
        {
            query = query.Where(x => x.OccurredOn <= end);
        }

        var amounts = await query.Select(x => x.Amount).ToListAsync(cancellationToken);
        return amounts.Sum();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, decimal>> GetAccountNetAmountsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _context.Transactions
            .AsNoTracking()
            .Select(x => new { x.AccountId, x.Type, x.Amount })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(x => x.AccountId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Type == TransactionType.Income ? x.Amount : -x.Amount));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateOnly>> GetDistinctMonthsAsync(CancellationToken cancellationToken = default)
    {
        var dates = await _context.Transactions
            .AsNoTracking()
            .Select(x => x.OccurredOn)
            .Distinct()
            .ToListAsync(cancellationToken);

        return dates
            .Select(d => new DateOnly(d.Year, d.Month, 1))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();
    }

    private IQueryable<Transaction> BuildQuery(TransactionQuery query)
    {
        var source = _context.Transactions
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Account)
            .AsQueryable();

        if (query.From is { } from)
        {
            source = source.Where(x => x.OccurredOn >= from);
        }

        if (query.To is { } to)
        {
            source = source.Where(x => x.OccurredOn <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.RemarkKeyword))
        {
            var keyword = query.RemarkKeyword.Trim();
            source = source.Where(x => x.Remark != null && x.Remark.Contains(keyword));
        }

        if (query.CategoryId is { } categoryId)
        {
            source = source.Where(x => x.CategoryId == categoryId);
        }

        if (query.AccountId is { } accountId)
        {
            source = source.Where(x => x.AccountId == accountId);
        }

        if (query.Type is { } type)
        {
            source = source.Where(x => x.Type == type);
        }

        return source;
    }
}
