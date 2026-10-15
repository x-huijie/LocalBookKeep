using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Repositories;

namespace LocalBookKeep.Application.Services;

/// <summary>
/// 账户用例：查询余额、改名、维护期初余额。
/// </summary>
/// <remarks>
/// 账户当前余额由“期初余额 + 收入合计 - 支出合计”实时推导，
/// 因此新增账单会自动减少/增加账户余额，修改与删除账单同样立即生效。
/// </remarks>
public sealed class AccountService
{
    /// <summary>账户名称长度上限。</summary>
    public const int MaxNameLength = 20;

    /// <summary>期初余额绝对值上限。</summary>
    public const decimal MaxBalance = 99_999_999.99m;

    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    /// <summary>构造账户服务。</summary>
    public AccountService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    /// <summary>查询全部账户及当前余额。</summary>
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.GetAllAsync(cancellationToken);
        var netAmounts = await _transactions.GetAccountNetAmountsAsync(cancellationToken);

        var result = new List<AccountDto>(accounts.Count);
        foreach (var account in accounts)
        {
            var net = netAmounts.TryGetValue(account.Id, out var value) ? value : 0m;
            result.Add(new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                InitialBalance = account.InitialBalance,
                CurrentBalance = account.InitialBalance + net,
                IsBuiltIn = account.IsBuiltIn,
                SortOrder = account.SortOrder,
                TransactionCount = await _accounts.CountTransactionsAsync(account.Id, cancellationToken)
            });
        }

        return result;
    }

    /// <summary>新增账户。</summary>
    public async Task<int> CreateAsync(
        string name,
        decimal initialBalance,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(name);
        ValidateBalance(initialBalance);

        if (await _accounts.ExistsAsync(normalized, null, cancellationToken))
        {
            throw new ValidationException($"已存在账户「{normalized}」。");
        }

        var existing = await _accounts.GetAllAsync(cancellationToken);
        var entity = new Account
        {
            Name = normalized,
            InitialBalance = decimal.Round(initialBalance, 2),
            IsBuiltIn = false,
            SortOrder = existing.Count == 0 ? 1 : existing.Max(a => a.SortOrder) + 1
        };

        await _accounts.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    /// <summary>修改账户名称与期初余额。</summary>
    public async Task UpdateAsync(
        int id,
        string name,
        decimal initialBalance,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(name);
        ValidateBalance(initialBalance);

        var entity = await _accounts.GetByIdAsync(id, cancellationToken)
                     ?? throw new ValidationException("账户不存在，可能已被删除。");

        if (!string.Equals(entity.Name, normalized, StringComparison.Ordinal)
            && await _accounts.ExistsAsync(normalized, id, cancellationToken))
        {
            throw new ValidationException($"已存在账户「{normalized}」。");
        }

        entity.Name = normalized;
        entity.InitialBalance = decimal.Round(initialBalance, 2);
        await _accounts.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>删除自定义账户；内置账户或已被账单引用的账户不允许删除。</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _accounts.GetByIdAsync(id, cancellationToken)
                     ?? throw new ValidationException("账户不存在，可能已被删除。");

        if (entity.IsBuiltIn)
        {
            throw new ValidationException($"「{entity.Name}」是内置账户，不能删除，可以修改名称。");
        }

        var used = await _accounts.CountTransactionsAsync(id, cancellationToken);
        if (used > 0)
        {
            throw new ValidationException($"「{entity.Name}」已被 {used} 条账单使用，请先修改这些账单的账户。");
        }

        await _accounts.DeleteAsync(id, cancellationToken);
    }

    private static string NormalizeName(string? name)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ValidationException("账户名称不能为空。");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new ValidationException($"账户名称最多 {MaxNameLength} 个字符。");
        }

        return normalized;
    }

    private static void ValidateBalance(decimal initialBalance)
    {
        if (decimal.Round(initialBalance, 2) != initialBalance)
        {
            throw new ValidationException("期初余额最多保留两位小数。");
        }

        if (Math.Abs(initialBalance) > MaxBalance)
        {
            throw new ValidationException($"期初余额绝对值不能超过 {MaxBalance:N2}。");
        }
    }
}
