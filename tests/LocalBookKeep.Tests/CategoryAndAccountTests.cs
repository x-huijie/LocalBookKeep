using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Domain.Enums;

namespace LocalBookKeep.Tests;

/// <summary>
/// 分类管理与账户管理的行为验证。
/// </summary>
public sealed class CategoryAndAccountTests
{
    [Fact]
    public async Task 初始化后写入内置分类与账户()
    {
        using var db = new TestDatabase();

        var expenses = await db.Categories.GetByTypeAsync(TransactionType.Expense);
        var incomes = await db.Categories.GetByTypeAsync(TransactionType.Income);
        var accounts = await db.Accounts.GetAllAsync();

        Assert.Equal(["餐饮", "交通", "房租", "购物", "医疗", "娱乐"], expenses.Select(x => x.Name).ToArray());
        Assert.Equal(["工资", "奖金", "兼职"], incomes.Select(x => x.Name).ToArray());
        Assert.Equal(["微信", "支付宝", "现金", "银行卡"], accounts.Select(x => x.Name).ToArray());
        Assert.All(expenses, x => Assert.True(x.IsBuiltIn));
        Assert.All(accounts, x => Assert.True(x.IsBuiltIn));
    }

    [Fact]
    public async Task 支出分类列表不会出现收入分类()
    {
        using var db = new TestDatabase();

        await db.Categories.CreateAsync("宠物", TransactionType.Expense);

        var expenses = await db.Categories.GetByTypeAsync(TransactionType.Expense);
        var incomes = await db.Categories.GetByTypeAsync(TransactionType.Income);

        Assert.Contains(expenses, x => x.Name == "宠物");
        Assert.DoesNotContain(incomes, x => x.Name == "宠物");
        Assert.DoesNotContain(incomes, x => x.Name == "餐饮");
    }

    [Fact]
    public async Task 同名分类在相同收支类型下不允许重复但可跨类型存在()
    {
        using var db = new TestDatabase();

        await db.Categories.CreateAsync("投资", TransactionType.Income);
        await Assert.ThrowsAsync<ValidationException>(
            () => db.Categories.CreateAsync("投资", TransactionType.Income));

        var expenseId = await db.Categories.CreateAsync("投资", TransactionType.Expense);
        Assert.True(expenseId > 0);
    }

    [Fact]
    public async Task 可以新增修改删除自定义分类()
    {
        using var db = new TestDatabase();

        var id = await db.Categories.CreateAsync("宠物", TransactionType.Expense);
        await db.Categories.RenameAsync(id, "宠物用品");

        var renamed = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Id == id);
        Assert.Equal("宠物用品", renamed.Name);

        await db.Categories.DeleteAsync(id);
        Assert.DoesNotContain(await db.Categories.GetByTypeAsync(TransactionType.Expense), x => x.Id == id);
    }

    [Fact]
    public async Task 内置分类不能删除()
    {
        using var db = new TestDatabase();
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        var error = await Assert.ThrowsAsync<ValidationException>(() => db.Categories.DeleteAsync(food.Id));
        Assert.Contains("内置分类", error.Message);
    }

    [Fact]
    public async Task 已被账单使用的分类不能删除()
    {
        using var db = new TestDatabase();
        var id = await db.Categories.CreateAsync("宠物", TransactionType.Expense);
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");

        await db.Transactions.CreateAsync(new Application.Dtos.TransactionInput
        {
            Type = TransactionType.Expense,
            Amount = 60m,
            CategoryId = id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 5),
            Remark = "狗粮"
        });

        var error = await Assert.ThrowsAsync<ValidationException>(() => db.Categories.DeleteAsync(id));
        Assert.Contains("已被 1 条账单使用", error.Message);
    }

    [Fact]
    public async Task 可以修改账户名称与期初余额()
    {
        using var db = new TestDatabase();
        var wechat = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "微信");

        await db.Accounts.UpdateAsync(wechat.Id, "微信零钱", 300m);

        var updated = (await db.Accounts.GetAllAsync()).Single(x => x.Id == wechat.Id);
        Assert.Equal("微信零钱", updated.Name);
        Assert.Equal(300m, updated.InitialBalance);
        Assert.Equal(300m, updated.CurrentBalance);
    }

    [Fact]
    public async Task 账户名称不能重复()
    {
        using var db = new TestDatabase();
        var wechat = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "微信");

        await Assert.ThrowsAsync<ValidationException>(() => db.Accounts.UpdateAsync(wechat.Id, "支付宝", 0m));
    }

    [Fact]
    public async Task 内置账户不能删除而自定义账户可以删除()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");

        var error = await Assert.ThrowsAsync<ValidationException>(() => db.Accounts.DeleteAsync(cash.Id));
        Assert.Contains("内置账户", error.Message);

        var id = await db.Accounts.CreateAsync("信用卡", -1500m);
        await db.Accounts.DeleteAsync(id);
        Assert.DoesNotContain(await db.Accounts.GetAllAsync(), x => x.Id == id);
    }

    [Fact]
    public async Task 空名称或超长名称被拒绝()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<ValidationException>(() => db.Categories.CreateAsync("   ", TransactionType.Expense));
        await Assert.ThrowsAsync<ValidationException>(
            () => db.Categories.CreateAsync(new string('长', CategoryService.MaxNameLength + 1), TransactionType.Expense));
        await Assert.ThrowsAsync<ValidationException>(() => db.Accounts.CreateAsync(string.Empty, 0m));
    }
}
