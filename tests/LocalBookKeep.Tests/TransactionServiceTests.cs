using LocalBookKeep.Application.Dtos;
using LocalBookKeep.Application.Exceptions;
using LocalBookKeep.Application.Services;
using LocalBookKeep.Domain.Enums;
using LocalBookKeep.Domain.Repositories;

namespace LocalBookKeep.Tests;

/// <summary>
/// 账单录入 / 修改 / 删除 / 查询 / 汇总的业务行为验证。
/// </summary>
public sealed class TransactionServiceTests
{
    private static readonly DateOnly TestMonth = new(2026, 3, 1);

    [Fact]
    public async Task 新增支出账单后账户余额自动扣减()
    {
        using var db = new TestDatabase();
        var wechat = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "微信");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(NewExpense(25.50m, food.Id, wechat.Id, new DateOnly(2026, 3, 5), "午饭"));

        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == wechat.Id);
        Assert.Equal(-25.50m, account.CurrentBalance);
    }

    [Fact]
    public async Task 新增收入账单后账户余额自动增加()
    {
        using var db = new TestDatabase();
        var bank = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "银行卡");
        var salary = (await db.Categories.GetByTypeAsync(TransactionType.Income)).Single(x => x.Name == "工资");

        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Income,
            Amount = 12000m,
            CategoryId = salary.Id,
            AccountId = bank.Id,
            OccurredOn = new DateOnly(2026, 3, 10),
            Remark = "3月工资"
        });

        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == bank.Id);
        Assert.Equal(12000m, account.CurrentBalance);
    }

    [Fact]
    public async Task 账户余额等于期初余额加收入减支出()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var expense = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "交通");
        var income = (await db.Categories.GetByTypeAsync(TransactionType.Income)).Single(x => x.Name == "兼职");

        await db.Accounts.UpdateAsync(cash.Id, cash.Name, 500m);
        await db.Transactions.CreateAsync(NewExpense(30m, expense.Id, cash.Id, new DateOnly(2026, 3, 6), "地铁"));
        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Income,
            Amount = 200m,
            CategoryId = income.Id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 7),
            Remark = "稿费"
        });

        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == cash.Id);
        Assert.Equal(500m + 200m - 30m, account.CurrentBalance);
    }

    [Fact]
    public async Task 金额为零或负数时拒绝录入()
    {
        using var db = new TestDatabase();
        var accountId = (await db.Accounts.GetAllAsync())[0].Id;
        var categoryId = (await db.Categories.GetByTypeAsync(TransactionType.Expense))[0].Id;

        foreach (var amount in new[] { 0m, -1m })
        {
            var error = await Assert.ThrowsAsync<ValidationException>(
                () => db.Transactions.CreateAsync(NewExpense(amount, categoryId, accountId, new DateOnly(2026, 3, 5), null)));
            Assert.Contains("大于 0", error.Message);
        }
    }

    [Fact]
    public async Task 分类与收支类型不匹配时拒绝录入()
    {
        using var db = new TestDatabase();
        var accountId = (await db.Accounts.GetAllAsync())[0].Id;
        var incomeCategoryId = (await db.Categories.GetByTypeAsync(TransactionType.Income))[0].Id;

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => db.Transactions.CreateAsync(NewExpense(10m, incomeCategoryId, accountId, new DateOnly(2026, 3, 5), null)));

        Assert.Contains("不匹配", error.Message);
    }

    [Fact]
    public async Task 修改账单后汇总与账户余额同步更新()
    {
        using var db = new TestDatabase();
        var wechat = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "微信");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");
        var rent = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "房租");

        var id = await db.Transactions.CreateAsync(NewExpense(25m, food.Id, wechat.Id, new DateOnly(2026, 3, 5), "午饭"));

        await db.Transactions.UpdateAsync(new TransactionInput
        {
            Id = id,
            Type = TransactionType.Expense,
            Amount = 2500m,
            CategoryId = rent.Id,
            AccountId = wechat.Id,
            OccurredOn = new DateOnly(2026, 3, 5),
            Remark = "3月房租"
        });

        var updated = await db.Transactions.GetAsync(id);
        Assert.NotNull(updated);
        Assert.Equal(2500m, updated!.Amount);
        Assert.Equal("房租", updated.CategoryName);
        Assert.Equal("3月房租", updated.Remark);

        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == wechat.Id);
        Assert.Equal(-2500m, account.CurrentBalance);
    }

    [Fact]
    public async Task 删除账单后账户余额恢复()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var traffic = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "交通");

        var id = await db.Transactions.CreateAsync(NewExpense(88m, traffic.Id, cash.Id, new DateOnly(2026, 3, 8), "加油"));
        await db.Transactions.DeleteAsync(id);

        Assert.Null(await db.Transactions.GetAsync(id));
        var account = (await db.Accounts.GetAllAsync()).Single(x => x.Id == cash.Id);
        Assert.Equal(0m, account.CurrentBalance);
    }

    [Fact]
    public async Task 账单列表按发生日期倒序排列()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(NewExpense(10m, food.Id, cash.Id, new DateOnly(2026, 3, 2), "较早"));
        await db.Transactions.CreateAsync(NewExpense(20m, food.Id, cash.Id, new DateOnly(2026, 3, 20), "较晚"));
        await db.Transactions.CreateAsync(NewExpense(30m, food.Id, cash.Id, new DateOnly(2026, 3, 9), "居中"));

        var list = await db.Transactions.QueryAsync(TransactionQuery.ForMonth(TestMonth));

        Assert.Equal(["较晚", "居中", "较早"], list.Select(x => x.Remark!).ToArray());
    }

    [Fact]
    public async Task 按月份筛选只返回该月账单()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(NewExpense(10m, food.Id, cash.Id, new DateOnly(2026, 2, 28), "2月最后一天"));
        await db.Transactions.CreateAsync(NewExpense(20m, food.Id, cash.Id, new DateOnly(2026, 3, 1), "3月第一天"));
        await db.Transactions.CreateAsync(NewExpense(30m, food.Id, cash.Id, new DateOnly(2026, 3, 31), "3月最后一天"));
        await db.Transactions.CreateAsync(NewExpense(40m, food.Id, cash.Id, new DateOnly(2026, 4, 1), "4月第一天"));

        var march = await db.Transactions.QueryAsync(TransactionQuery.ForMonth(TestMonth));

        Assert.Equal(["3月最后一天", "3月第一天"], march.Select(x => x.Remark!).ToArray());
    }

    [Fact]
    public async Task 按备注关键字搜索账单()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(NewExpense(10m, food.Id, cash.Id, new DateOnly(2026, 3, 2), "晚饭"));
        await db.Transactions.CreateAsync(NewExpense(20m, food.Id, cash.Id, new DateOnly(2026, 3, 3), "加油"));
        await db.Transactions.CreateAsync(NewExpense(30m, food.Id, cash.Id, new DateOnly(2026, 3, 4), "晚饭和加班餐"));

        var result = await db.Transactions.QueryAsync(new TransactionQuery { RemarkKeyword = "晚饭" });

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Contains("晚饭", x.Remark!));
    }

    [Fact]
    public async Task 月度汇总统计收入支出与结余()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");
        var salary = (await db.Categories.GetByTypeAsync(TransactionType.Income)).Single(x => x.Name == "工资");

        await db.Transactions.CreateAsync(NewExpense(120m, food.Id, cash.Id, new DateOnly(2026, 3, 5), "聚餐"));
        await db.Transactions.CreateAsync(NewExpense(80m, food.Id, cash.Id, new DateOnly(2026, 3, 6), "外卖"));
        await db.Transactions.CreateAsync(NewExpense(999m, food.Id, cash.Id, new DateOnly(2026, 4, 1), "下月不计入"));
        await db.Transactions.CreateAsync(new TransactionInput
        {
            Type = TransactionType.Income,
            Amount = 5000m,
            CategoryId = salary.Id,
            AccountId = cash.Id,
            OccurredOn = new DateOnly(2026, 3, 10)
        });

        var summary = await db.Summary.GetSummaryAsync(TestMonth);

        Assert.Equal(5000m, summary.TotalIncome);
        Assert.Equal(200m, summary.TotalExpense);
        Assert.Equal(4800m, summary.Balance);
    }

    [Fact]
    public async Task 月份下拉列表包含有账单的月份与当前月份()
    {
        using var db = new TestDatabase();
        var cash = (await db.Accounts.GetAllAsync()).Single(x => x.Name == "现金");
        var food = (await db.Categories.GetByTypeAsync(TransactionType.Expense)).Single(x => x.Name == "餐饮");

        await db.Transactions.CreateAsync(NewExpense(10m, food.Id, cash.Id, new DateOnly(2025, 12, 31), null));

        var months = await db.Summary.GetAvailableMonthsAsync();

        Assert.Contains(new DateOnly(2025, 12, 1), months);
        Assert.Contains(SummaryService.CurrentMonth(), months);
        Assert.Equal(months.OrderByDescending(x => x), months);
    }

    private static TransactionInput NewExpense(
        decimal amount,
        int categoryId,
        int accountId,
        DateOnly occurredOn,
        string? remark) => new()
        {
            Type = TransactionType.Expense,
            Amount = amount,
            CategoryId = categoryId,
            AccountId = accountId,
            OccurredOn = occurredOn,
            Remark = remark
        };
}
