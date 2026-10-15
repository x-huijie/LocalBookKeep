using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalBookKeep.Infrastructure.Persistence;

/// <summary>
/// 数据库初始化：建库 + 写入内置分类与账户。
/// </summary>
public sealed class DatabaseInitializer
{
    /// <summary>内置支出分类。</summary>
    public static readonly string[] DefaultExpenseCategories =
        ["餐饮", "交通", "房租", "购物", "医疗", "娱乐"];

    /// <summary>内置收入分类。</summary>
    public static readonly string[] DefaultIncomeCategories =
        ["工资", "奖金", "兼职"];

    /// <summary>内置账户。</summary>
    public static readonly string[] DefaultAccounts =
        ["微信", "支付宝", "现金", "银行卡"];

    private readonly BookKeepDbContext _context;

    /// <summary>构造初始化器。</summary>
    public DatabaseInitializer(BookKeepDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 确保数据库结构存在，并补齐缺失的内置分类 / 账户。
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.EnsureCreatedAsync(cancellationToken);
        await SeedCategoriesAsync(cancellationToken);
        await SeedAccountsAsync(cancellationToken);
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        // 只在某一收支类型下完全没有分类时才写入默认值，
        // 避免用户清空后改名又被自动补回。
        if (!await _context.Categories.AnyAsync(x => x.Type == TransactionType.Expense, cancellationToken))
        {
            await AddCategoriesAsync(DefaultExpenseCategories, TransactionType.Expense, cancellationToken);
        }

        if (!await _context.Categories.AnyAsync(x => x.Type == TransactionType.Income, cancellationToken))
        {
            await AddCategoriesAsync(DefaultIncomeCategories, TransactionType.Income, cancellationToken);
        }
    }

    private async Task AddCategoriesAsync(
        IEnumerable<string> names,
        TransactionType type,
        CancellationToken cancellationToken)
    {
        var sort = 1;
        foreach (var name in names)
        {
            _context.Categories.Add(new Category
            {
                Name = name,
                Type = type,
                IsBuiltIn = true,
                SortOrder = sort++
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAccountsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var sort = 1;
        foreach (var name in DefaultAccounts)
        {
            _context.Accounts.Add(new Account
            {
                Name = name,
                InitialBalance = 0m,
                IsBuiltIn = true,
                SortOrder = sort++
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
