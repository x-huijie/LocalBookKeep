using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Domain.Entities;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;

namespace LocalBookKeep.Application.Services;

/// <summary>
/// 账单用例：录入、修改、删除、查询。
/// </summary>
public sealed class TransactionService
{
    /// <summary>单笔金额上限。</summary>
    public const decimal MaxAmount = 99_999_999.99m;

    /// <summary>备注长度上限。</summary>
    public const int MaxRemarkLength = 200;

    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;
    private readonly IAccountRepository _accounts;

    /// <summary>构造账单服务。</summary>
    public TransactionService(
        ITransactionRepository transactions,
        ICategoryRepository categories,
        IAccountRepository accounts)
    {
        _transactions = transactions;
        _categories = categories;
        _accounts = accounts;
    }

    /// <summary>按条件查询账单（时间倒序）。</summary>
    public async Task<IReadOnlyList<TransactionDto>> QueryAsync(
        TransactionQuery query,
        CancellationToken cancellationToken = default)
    {
        var list = await _transactions.QueryAsync(query, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    /// <summary>查询全部账单。</summary>
    public async Task<IReadOnlyList<TransactionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _transactions.GetAllAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    /// <summary>按主键查询账单。</summary>
    public async Task<TransactionDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _transactions.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    /// <summary>新增账单。</summary>
    public async Task<int> CreateAsync(TransactionInput input, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(input, cancellationToken);

        var now = DateTime.Now;
        var entity = new Transaction
        {
            Type = input.Type,
            Amount = decimal.Round(input.Amount, 2),
            CategoryId = input.CategoryId,
            AccountId = input.AccountId,
            OccurredOn = input.OccurredOn,
            Remark = NormalizeRemark(input.Remark),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _transactions.AddAsync(entity, cancellationToken);
        return entity.Id;
    }

    /// <summary>修改账单。</summary>
    public async Task UpdateAsync(TransactionInput input, CancellationToken cancellationToken = default)
    {
        if (input.Id <= 0)
        {
            throw new ValidationException("要修改的账单不存在。");
        }

        await ValidateAsync(input, cancellationToken);

        var entity = await _transactions.GetByIdAsync(input.Id, cancellationToken)
                     ?? throw new ValidationException("账单不存在，可能已被删除，请刷新后重试。");

        entity.Type = input.Type;
        entity.Amount = decimal.Round(input.Amount, 2);
        entity.CategoryId = input.CategoryId;
        entity.AccountId = input.AccountId;
        entity.OccurredOn = input.OccurredOn;
        entity.Remark = NormalizeRemark(input.Remark);
        entity.UpdatedAt = DateTime.Now;

        await _transactions.UpdateAsync(entity, cancellationToken);
    }

    /// <summary>删除账单。</summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("要删除的账单不存在。");
        }

        await _transactions.DeleteAsync(id, cancellationToken);
    }

    private async Task ValidateAsync(TransactionInput input, CancellationToken cancellationToken)
    {
        if (input.Amount <= 0)
        {
            throw new ValidationException("请输入大于 0 的金额。");
        }

        if (input.Amount > MaxAmount)
        {
            throw new ValidationException($"单笔金额不能超过 {MaxAmount:N2}。");
        }

        if (decimal.Round(input.Amount, 2) != input.Amount)
        {
            throw new ValidationException("金额最多保留两位小数。");
        }

        if (input.OccurredOn == default)
        {
            throw new ValidationException("请选择发生日期。");
        }

        var remark = NormalizeRemark(input.Remark);
        if (remark is not null && remark.Length > MaxRemarkLength)
        {
            throw new ValidationException($"备注最多 {MaxRemarkLength} 个字符。");
        }

        var category = await _categories.GetByIdAsync(input.CategoryId, cancellationToken)
                       ?? throw new ValidationException("所选分类不存在，请重新选择。");

        if (category.Type != input.Type)
        {
            throw new ValidationException($"「{category.Name}」属于{category.Type.ToDisplayName()}分类，与当前收支类型不匹配。");
        }

        var account = await _accounts.GetByIdAsync(input.AccountId, cancellationToken);
        if (account is null)
        {
            throw new ValidationException("所选账户不存在，请重新选择。");
        }
    }

    private static string? NormalizeRemark(string? remark)
    {
        if (string.IsNullOrWhiteSpace(remark))
        {
            return null;
        }

        return remark.Trim();
    }

    private static TransactionDto ToDto(Transaction entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Amount = entity.Amount,
        OccurredOn = entity.OccurredOn,
        Remark = entity.Remark,
        CategoryId = entity.CategoryId,
        CategoryName = entity.Category?.Name ?? string.Empty,
        AccountId = entity.AccountId,
        AccountName = entity.Account?.Name ?? string.Empty
    };
}
