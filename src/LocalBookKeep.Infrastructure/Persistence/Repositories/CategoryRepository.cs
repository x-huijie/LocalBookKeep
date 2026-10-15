using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Persistence.Repositories;

/// <summary>
/// 基于 EF Core 的分类仓储实现。
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly BookKeepDbContext _context;

    /// <summary>构造分类仓储。</summary>
    public CategoryRepository(BookKeepDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Type)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetByTypeAsync(
        TransactionType type,
        CancellationToken cancellationToken = default)
        => await _context.Categories
            .AsNoTracking()
            .Where(x => x.Type == type)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(
        string name,
        TransactionType type,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
        => _context.Categories.AnyAsync(
            x => x.Name == name && x.Type == type && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    /// <inheritdoc />
    public Task<int> CountTransactionsAsync(int categoryId, CancellationToken cancellationToken = default)
        => _context.Transactions.CountAsync(x => x.CategoryId == categoryId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _context.Categories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
