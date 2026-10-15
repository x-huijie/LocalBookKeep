using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Persistence.Repositories;

/// <summary>
/// 基于 EF Core 的账户仓储实现。
/// </summary>
public sealed class AccountRepository : IAccountRepository
{
    private readonly BookKeepDbContext _context;

    /// <summary>构造账户仓储。</summary>
    public AccountRepository(BookKeepDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Accounts
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        => _context.Accounts.AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    /// <inheritdoc />
    public Task<int> CountTransactionsAsync(int accountId, CancellationToken cancellationToken = default)
        => _context.Transactions.CountAsync(x => x.AccountId == accountId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _context.Accounts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
